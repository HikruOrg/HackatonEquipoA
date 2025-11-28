using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Models;

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
        /// Retrieves unread LinkSV Pulse newsletters from Outlook inbox
        /// </summary>
        public async Task<List<string>> GetLinkSVPulseEmailsAsync(int maxEmails = 10)
        {
            try
            {
                _logger?.LogInformation("Fetching unread LinkSV Pulse emails from Outlook...");

                // Search for unread emails from LinkSV Pulse
                var messages = await _graphServiceClient.Users[_userId].Messages
                    .GetAsync(requestConfiguration =>
                    {
                        //requestConfiguration.QueryParameters.Filter = "isRead eq false";
                        requestConfiguration.QueryParameters.Top = maxEmails;
                        //requestConfiguration.QueryParameters.Orderby = new[] { "receivedDateTime desc" };
                        //requestConfiguration.QueryParameters.Select = new[] { "subject", "body", "from", "receivedDateTime", "isRead" };
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
                            
                            _logger?.LogInformation($"Retrieved unread email: {message.Subject} from {message.From?.EmailAddress?.Address}");
                        }
                    }
                }

                _logger?.LogInformation($"Successfully retrieved {emailContents.Count} unread LinkSV Pulse emails");
                return emailContents;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to retrieve emails from Outlook");
                throw new InvalidOperationException($"Failed to retrieve emails: {ex.Message}", ex);
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
}