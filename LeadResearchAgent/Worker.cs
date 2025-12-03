using LeadResearchAgent.Agents;
using LeadResearchAgent.Models;
using LeadResearchAgent.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;

namespace LeadResearchAgent
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly AzureFoundryLeadAgent _foundryAgent;
        private readonly GraphServiceClient _graphClient;
        private readonly IHostApplicationLifetime _hostApplicationLifetime;
        private readonly ILoggerFactory _loggerFactory;
        private readonly TimeSpan? _executionInterval;

        public Worker(
            ILogger<Worker> logger, 
            AzureFoundryLeadAgent foundryAgent,
            GraphServiceClient graphClient,
            IHostApplicationLifetime hostApplicationLifetime,
            ILoggerFactory loggerFactory)
        {
            _logger = logger;
            _foundryAgent = foundryAgent;
            _graphClient = graphClient;
            _hostApplicationLifetime = hostApplicationLifetime;
            _loggerFactory = loggerFactory;
            _executionInterval = ParseExecutionInterval();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // If no interval configured, run once and stop (triggered WebJob mode)
            if (!_executionInterval.HasValue)
            {
                _logger.LogInformation("Running in triggered mode (no interval configured)");
                await RunProcessingAsync(stoppingToken);
                _hostApplicationLifetime.StopApplication();
                return;
            }

            // Interval mode - run continuously at specified intervals
            _logger.LogInformation("Running in interval mode. Execution interval: {Interval}", _executionInterval.Value);

            while (!stoppingToken.IsCancellationRequested)
            {
                await RunProcessingAsync(stoppingToken);

                if (!stoppingToken.IsCancellationRequested)
                {
                    _logger.LogInformation("Next execution in {Interval}. Waiting...", _executionInterval.Value);
                    await Task.Delay(_executionInterval.Value, stoppingToken);
                }
            }
        }

        private async Task RunProcessingAsync(CancellationToken stoppingToken)
        {
            try
            {
                _logger.LogInformation("Lead Research Agent - starting execution at {Time}", DateTime.Now);

                var userId = Environment.GetEnvironmentVariable("MICROSOFT_GRAPH_USER_ID") ?? "me";
                var outlookService = new OutlookEmailService(_graphClient, userId, _loggerFactory.CreateLogger<OutlookEmailService>());

                var emails = await LoadOutlookEmailsAsync(outlookService, stoppingToken);

                var firstEmail = emails?.FirstOrDefault();
                if (firstEmail != null)
                {
                    await ProcessNewsletterWithFoundryAsync(outlookService, firstEmail.Content, stoppingToken);
                    
                    // Mark email as read after successful processing
                    await outlookService.MarkEmailAsProcessedAsync(firstEmail.Id);
                }

                _logger.LogInformation("Processing completed successfully at {Time}", DateTime.Now);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during processing: {Message}", ex.Message);
            }
        }

        private async Task ProcessNewsletterWithFoundryAsync(OutlookEmailService outlookService, string? email, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(email))
            {
                _logger.LogWarning("No email content to process");
                return;
            }

            var results = await _foundryAgent.ProcessNewsletterAsync(email, cancellationToken: cancellationToken);

            await outlookService.SendResultsEmailAsync("daniel.ramirez@hikrutech.com", results, cancellationToken);
        }

        private async Task<List<EmailMessage>> LoadOutlookEmailsAsync(OutlookEmailService outlookService, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("📧 Connecting to Outlook...");

                _logger.LogInformation("🔍 Searching for LinkSV Pulse newsletters...");
                var emails = await outlookService.GetLinkSVPulseEmailsAsync(10);

                if (!emails.Any())
                {
                    _logger.LogWarning("❌ No LinkSV Pulse emails found. Please check:");
                    _logger.LogWarning("   - You have LinkSV Pulse emails in your inbox");
                    _logger.LogWarning("   - Your Microsoft Graph permissions are correctly configured");
                    return new List<EmailMessage>();
                }

                _logger.LogInformation("✅ Found {Count} LinkSV Pulse newsletters", emails.Count);

                return emails;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to process Outlook emails: {Message}", ex.Message);
                _logger.LogInformation("💡 Falling back to demo mode with sample newsletter...");
                return new List<EmailMessage>();
            }
        }

        private TimeSpan? ParseExecutionInterval()
        {
            // Format: "60" (minutes) or "01:00:00" (hours:minutes:seconds)
            var intervalStr = Environment.GetEnvironmentVariable("WORKER_EXECUTION_INTERVAL");

            if (string.IsNullOrEmpty(intervalStr))
            {
                _logger.LogInformation("WORKER_EXECUTION_INTERVAL not configured. Running in triggered mode.");
                return null;
            }

            try
            {
                TimeSpan interval;

                // Try parsing as minutes first (format: "60")
                if (int.TryParse(intervalStr, out var minutes))
                {
                    interval = TimeSpan.FromMinutes(minutes);
                    _logger.LogInformation("Execution interval configured: {Interval} ({Minutes} minutes)", interval, minutes);
                }
                // Fall back to parsing as TimeSpan (format: "01:00:00")
                else if (TimeSpan.TryParse(intervalStr, out interval))
                {
                    _logger.LogInformation("Execution interval configured: {Interval}", interval);
                }
                else
                {
                    _logger.LogError("Failed to parse WORKER_EXECUTION_INTERVAL: '{Value}'. Format should be minutes as integer or 'HH:mm:ss'. Running in triggered mode.", intervalStr);
                    return null;
                }

                // Validate interval doesn't exceed Task.Delay maximum (approximately 24.85 days)
                var maxDelay = TimeSpan.FromMilliseconds(int.MaxValue);
                if (interval > maxDelay)
                {
                    _logger.LogError("WORKER_EXECUTION_INTERVAL of {Interval} exceeds maximum allowed delay of {MaxDelay}. Running in triggered mode.", interval, maxDelay);
                    return null;
                }

                return interval;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse WORKER_EXECUTION_INTERVAL. Running in triggered mode.");
                return null;
            }
        }
    }
}
