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
        private readonly TimeSpan? _executionTime;
        private readonly TimeZoneInfo _timeZone;
        private DateTime? _lastExecutionDate;

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
            _timeZone = ParseTimeZone();
            _executionTime = ParseExecutionTime();
            _executionInterval = ParseExecutionInterval();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // If no interval configured stop
            if (!_executionInterval.HasValue)
            {
                _hostApplicationLifetime.StopApplication();
                return;
            }

            // Interval mode - run continuously at specified intervals
            _logger.LogInformation("Running in interval mode. Execution interval: {Interval}", _executionInterval.Value);
            
            if (_executionTime.HasValue)
            {
                _logger.LogInformation("Execution will only occur at configured time: {Time} ({TimeZone})", 
                    _executionTime.Value.ToString(@"hh\:mm"), 
                    _timeZone.DisplayName);
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                if (ShouldExecuteNow())
                {
                    await RunProcessingAsync(stoppingToken);
                    _lastExecutionDate = GetCurrentDate();
                }
                else
                {
                    var currentTime = GetCurrentTime();
                    _logger.LogInformation("Current time {CurrentTime} ({TimeZone}) - waiting for execution time {ExecutionTime}", 
                        currentTime.ToString(@"hh\:mm"), 
                        _timeZone.DisplayName,
                        _executionTime?.ToString(@"hh\:mm") ?? "any");
                }

                if (!stoppingToken.IsCancellationRequested)
                {
                    _logger.LogInformation("Next check in {Interval}. Waiting...", _executionInterval.Value);
                    await Task.Delay(_executionInterval.Value, stoppingToken);
                }
            }
        }

        private bool ShouldExecuteNow()
        {
            // If no execution time is configured, never execute
            if (!_executionTime.HasValue)
            {
                return false;
            }

            var currentDateTime = GetCurrentDate();
            var currentTime = GetCurrentTime();

            // Extract the target hour from execution time
            var targetHour = _executionTime.Value.Hours;
            var currentHour = currentTime.Hours;

            // Check if current hour matches target hour
            if (currentHour != targetHour)
            {
                return false;
            }

            // We're in the correct hour - now check if we already executed in this hour
            if (_lastExecutionDate.HasValue)
            {
                var lastExecutionHour = _lastExecutionDate.Value.Hour;
                var lastExecutionDay = _lastExecutionDate.Value.Date;
                var currentDay = currentDateTime.Date;

                // If we already executed in this hour today, skip
                if (lastExecutionDay == currentDay && lastExecutionHour == currentHour)
                {
                    return false;
                }
            }

            // Check if current time is within tolerance of execution time
            var timeDifference = currentTime - _executionTime.Value;
            var tolerance = _executionInterval ?? TimeSpan.FromMinutes(1);

            if (Math.Abs(timeDifference.TotalMinutes) <= tolerance.TotalMinutes)
            {
                return true;
            }

            return false;
        }

        private DateTime GetCurrentDate()
        {
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _timeZone);
        }

        private TimeSpan GetCurrentTime()
        {
            var currentDateTime = GetCurrentDate();
            return currentDateTime.TimeOfDay;
        }

        private async Task RunProcessingAsync(CancellationToken stoppingToken)
        {
            try
            {
                var currentDateTime = GetCurrentDate();
                _logger.LogInformation("Lead Research Agent - starting execution at {Time} ({TimeZone})", 
                    currentDateTime, 
                    _timeZone.DisplayName);

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

                _logger.LogInformation("Processing completed successfully at {Time} ({TimeZone})", 
                    GetCurrentDate(), 
                    _timeZone.DisplayName);
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

            var recipientEmails = Environment.GetEnvironmentVariable("RECIPIENT_EMAILS");

            if (string.IsNullOrEmpty(recipientEmails))
            {
                _logger.LogError("RECIPIENT_EMAILS environment variable is not configured. Cannot send results email.");
                return;
            }

            var results = await _foundryAgent.ProcessNewsletterAsync(email, cancellationToken: cancellationToken);

            // Split by semicolon or comma to support multiple recipients
            var recipients = recipientEmails.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(e => e.Trim())
                .Where(e => !string.IsNullOrEmpty(e))
                .ToList();

            if (!recipients.Any())
            {
                _logger.LogError("No valid recipient emails found in RECIPIENT_EMAILS environment variable.");
                return;
            }

            foreach (var recipient in recipients)
            {
                await outlookService.SendResultsEmailAsync(recipient, results, cancellationToken);
                _logger.LogInformation("Results email sent to: {Recipient}", recipient);
            }
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

        private TimeZoneInfo ParseTimeZone()
        {
            var timeZoneId = Environment.GetEnvironmentVariable("WORKER_TIMEZONE");

            if (string.IsNullOrEmpty(timeZoneId))
            {
                _logger.LogInformation("WORKER_TIMEZONE not configured. Using UTC timezone.");
                return TimeZoneInfo.Utc;
            }

            try
            {
                var timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
                _logger.LogInformation("Timezone configured: {TimeZone} (UTC{Offset})", 
                    timeZone.DisplayName, 
                    timeZone.BaseUtcOffset.ToString(@"hh\:mm"));
                return timeZone;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse WORKER_TIMEZONE: '{Value}'. Using UTC timezone.", timeZoneId);
                return TimeZoneInfo.Utc;
            }
        }

        private TimeSpan? ParseExecutionTime()
        {
            var timeStr = Environment.GetEnvironmentVariable("WORKER_EXECUTION_TIME");

            if (string.IsNullOrEmpty(timeStr))
            {
                return null;
            }

            try
            {
                if (TimeSpan.TryParseExact(timeStr, @"hh\:mm", null, out var time))
                {
                    if (time.Hours >= 0 && time.Hours < 24)
                    {
                        _logger.LogInformation("Execution time configured: {Time} ({TimeZone})", 
                            time.ToString(@"hh\:mm"), 
                            _timeZone.DisplayName);
                        return time;
                    }
                }

                _logger.LogError("Failed to parse WORKER_EXECUTION_TIME: '{Value}'. Format should be 'HH:mm' (e.g., '14:30').", timeStr);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse WORKER_EXECUTION_TIME.");
                return null;
            }
        }

        private TimeSpan? ParseExecutionInterval()
        {
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
