using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Extensions.Logging;

namespace LeadResearchAgent.Services
{
    public class OutlookEmailService
    {
        private readonly GraphServiceClient _graphServiceClient;
        private readonly ILogger<OutlookEmailService>? _logger;
        private readonly string _userId;

        public OutlookEmailService(GraphServiceClient graphServiceClient, string userId = "me", ILogger<OutlookEmailService>? logger = null)
        {
            _graphServiceClient = graphServiceClient;
            _userId = userId;
            _logger = logger;
        }

        /// <summary>
        /// Retrieves LinkSV Pulse newsletters from Outlook inbox
        /// </summary>
        public async Task<List<string>> GetLinkSVPulseEmailsAsync(int maxEmails = 10)
        {
            try
            {
                _logger?.LogInformation("Fetching LinkSV Pulse emails from Outlook...");

                // Search for emails from LinkSV Pulse
                var messages = await _graphServiceClient.Users[_userId].Messages
                    .GetAsync(requestConfiguration =>
                    {
                        //requestConfiguration.QueryParameters.Filter =
                        //    "startswith(subject,'LinkSV Pulse')";
                        requestConfiguration.QueryParameters.Top = maxEmails;
                        //requestConfiguration.QueryParameters.Orderby = new[] { "receivedDateTime desc" };
                        //requestConfiguration.QueryParameters.Select = new[] { "subject", "body", "from", "receivedDateTime" };
                    });

                var emailContents = new List<string>();

                if (messages?.Value != null)
                {
                    foreach (var message in messages.Value)
                    {
                        if (message.Body?.Content != null)
                        {
                            var emailContent = ExtractTextFromHtml(message.Body.Content);
                            emailContents.Add(emailContent);
                            
                            _logger?.LogInformation($"Retrieved email: {message.Subject} from {message.From?.EmailAddress?.Address}");
                        }
                    }
                }

                _logger?.LogInformation($"Successfully retrieved {emailContents.Count} LinkSV Pulse emails");
                return emailContents;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to retrieve emails from Outlook");
                throw new InvalidOperationException($"Failed to retrieve emails: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Searches for unread newsletter emails in the last 7 days
        /// </summary>
        public async Task<List<NewsletterEmail>> GetRecentNewslettersAsync(int days = 7)
        {
            try
            {
                var sinceDate = DateTime.UtcNow.AddDays(-days).ToString("yyyy-MM-ddTHH:mm:ssZ");
                
                var messages = await _graphServiceClient.Users[_userId].Messages
                    .GetAsync(requestConfiguration =>
                    {
                        requestConfiguration.QueryParameters.Filter = 
                            $"receivedDateTime ge {sinceDate} and " +
                            "(contains(subject,'newsletter') or " +
                            "contains(subject,'funding') or " +
                            "contains(subject,'startups') or " +
                            "contains(subject,'venture') or " +
                            "contains(subject,'investment') or " +
                            "contains(from/emailAddress/address,'linksv'))";
                        requestConfiguration.QueryParameters.Top = 50;
                        requestConfiguration.QueryParameters.Orderby = new[] { "receivedDateTime desc" };
                    });

                var newsletters = new List<NewsletterEmail>();

                if (messages?.Value != null)
                {
                    foreach (var message in messages.Value)
                    {
                        newsletters.Add(new NewsletterEmail
                        {
                            Subject = message.Subject ?? "",
                            Content = ExtractTextFromHtml(message.Body?.Content ?? ""),
                            Sender = message.From?.EmailAddress?.Address ?? "",
                            ReceivedDate = message.ReceivedDateTime?.DateTime ?? DateTime.Now,
                            MessageId = message.Id ?? ""
                        });
                    }
                }

                return newsletters;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to retrieve recent newsletters");
                throw;
            }
        }

        /// <summary>
        /// Marks processed emails as read to avoid reprocessing
        /// </summary>
        public async Task MarkEmailAsProcessedAsync(string messageId)
        {
            try
            {
                var message = new Message
                {
                    IsRead = true
                };

                await _graphServiceClient.Users[_userId].Messages[messageId]
                    .PatchAsync(message);

                _logger?.LogInformation($"Marked email {messageId} as read");
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, $"Failed to mark email {messageId} as read");
            }
        }

        /// <summary>
        /// Creates a folder for processed newsletters (optional organization)
        /// </summary>
        public async Task<string> CreateProcessedFolderAsync()
        {
            try
            {
                var folder = new MailFolder
                {
                    DisplayName = "Processed Newsletters - Hikru"
                };

                var createdFolder = await _graphServiceClient.Users[_userId].MailFolders
                    .PostAsync(folder);

                return createdFolder?.Id ?? "";
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to create processed folder");
                return "";
            }
        }

        private string ExtractTextFromHtml(string htmlContent)
        {
            if (string.IsNullOrEmpty(htmlContent))
                return "";

            // Simple HTML to text conversion
            // For production, consider using HtmlAgilityPack for better parsing
            var text = htmlContent
                .Replace("<br>", "\n")
                .Replace("<br/>", "\n")
                .Replace("<br />", "\n")
                .Replace("</p>", "\n")
                .Replace("</div>", "\n")
                .Replace("</h1>", "\n")
                .Replace("</h2>", "\n")
                .Replace("</h3>", "\n")
                .Replace("</h4>", "\n")
                .Replace("</h5>", "\n")
                .Replace("</h6>", "\n");

            // Remove HTML tags
            text = System.Text.RegularExpressions.Regex.Replace(text, "<.*?>", "");
            
            // Decode HTML entities
            text = System.Net.WebUtility.HtmlDecode(text);
            
            // Clean up whitespace
            text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ");
            text = System.Text.RegularExpressions.Regex.Replace(text, @"\n\s*\n", "\n\n");
            
            return text.Trim();
        }
    }

    public class NewsletterEmail
    {
        public string Subject { get; set; } = "";
        public string Content { get; set; } = "";
        public string Sender { get; set; } = "";
        public DateTime ReceivedDate { get; set; }
        public string MessageId { get; set; } = "";
    }
}