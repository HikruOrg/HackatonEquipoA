using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Extensions.Logging;
using LeadResearchAgent.Models;

namespace LeadResearchAgent.Services
{
    public class OutlookEmailService
    {
        private readonly GraphServiceClient _graphServiceClient;
        private readonly ILogger<OutlookEmailService>? _logger;

        public OutlookEmailService(GraphServiceClient graphServiceClient, ILogger<OutlookEmailService>? logger = null)
        {
            _graphServiceClient = graphServiceClient;
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
                var messages = await _graphServiceClient.Me.Messages
                    .GetAsync(requestConfiguration =>
                    {
                        requestConfiguration.QueryParameters.Filter = 
                            "contains(from/emailAddress/address,'linksv') or " +
                            "contains(subject,'LinkSV') or " +
                            "contains(subject,'Pulse') or " +
                            "contains(from/emailAddress/name,'LinkSV')";
                        requestConfiguration.QueryParameters.Top = maxEmails;
                        requestConfiguration.QueryParameters.Orderby = new[] { "receivedDateTime desc" };
                        requestConfiguration.QueryParameters.Select = new[] { "subject", "body", "from", "receivedDateTime" };
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
                
                var messages = await _graphServiceClient.Me.Messages
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

                await _graphServiceClient.Me.Messages[messageId]
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

                var createdFolder = await _graphServiceClient.Me.MailFolders
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

        /// <summary>
        /// Sends an email with outreach message for discovered leads
        /// </summary>
        public async Task<bool> SendOutreachEmailAsync(string recipientEmail, string subject, string body, List<Company>? companies = null)
        {
            try
            {
                _logger?.LogInformation($"Sending outreach email to {recipientEmail}");

                // Create email message
                var message = new Message
                {
                    Subject = subject,
                    Body = new ItemBody
                    {
                        ContentType = BodyType.Html,
                        Content = body
                    },
                    ToRecipients = new List<Recipient>
                    {
                        new Recipient
                        {
                            EmailAddress = new EmailAddress
                            {
                                Address = recipientEmail
                            }
                        }
                    }
                };

                // Add attachments if companies are provided
                if (companies != null && companies.Any())
                {
                    var attachmentContent = GenerateCompanyReportAttachment(companies);
                    var attachment = new FileAttachment
                    {
                        Name = $"lead_research_report_{DateTime.Now:yyyyMMdd_HHmmss}.json",
                        ContentType = "application/json",
                        ContentBytes = System.Text.Encoding.UTF8.GetBytes(attachmentContent)
                    };
                    message.Attachments = new List<Attachment> { attachment };
                }

                // Send the email
                await _graphServiceClient.Me.SendMail
                    .PostAsync(new Microsoft.Graph.Me.SendMail.SendMailPostRequestBody
                    {
                        Message = message,
                        SaveToSentItems = true
                    });

                _logger?.LogInformation("✅ Email sent successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"❌ Failed to send email to {recipientEmail}");
                Console.WriteLine($"❌ Failed to send email: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Sends a summary email with lead research results
        /// </summary>
        public async Task<bool> SendLeadSummaryEmailAsync(string recipientEmail, List<Company> companies, string sourceInfo = "")
        {
            try
            {
                var subject = $"🎯 Lead Research Results - {companies.Count} Companies Found ({DateTime.Now:yyyy-MM-dd})";
                
                var htmlBody = GenerateLeadSummaryHtml(companies, sourceInfo);
                
                return await SendOutreachEmailAsync(recipientEmail, subject, htmlBody, companies);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to send lead summary email");
                return false;
            }
        }

        private string GenerateLeadSummaryHtml(List<Company> companies, string sourceInfo)
        {
            var html = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 20px; }}
        .header {{ background-color: #0078d4; color: white; padding: 20px; border-radius: 5px; }}
        .company {{ border: 1px solid #ddd; margin: 10px 0; padding: 15px; border-radius: 5px; }}
        .high-score {{ border-left: 5px solid #28a745; }}
        .medium-score {{ border-left: 5px solid #ffc107; }}
        .low-score {{ border-left: 5px solid #dc3545; }}
        .score {{ font-weight: bold; font-size: 1.2em; }}
        .details {{ margin-top: 10px; }}
        .funding {{ color: #28a745; font-weight: bold; }}
        .location {{ color: #6c757d; }}
        .footer {{ margin-top: 30px; padding: 15px; background-color: #f8f9fa; border-radius: 5px; }}
    </style>
</head>
<body>
    <div class='header'>
        <h1>🎯 Lead Research Results</h1>
        <p>Processed by Hikru Lead Research Agent</p>
        {(!string.IsNullOrEmpty(sourceInfo) ? $"<p>Source: {sourceInfo}</p>" : "")}
    </div>

    <h2>📊 Summary</h2>
    <ul>
        <li><strong>Total Companies Found:</strong> {companies.Count}</li>
        <li><strong>High ICP Score (≥0.7):</strong> {companies.Count(c => c.ICPScore >= 0.7)}</li>
        <li><strong>Medium ICP Score (0.4-0.7):</strong> {companies.Count(c => c.ICPScore >= 0.4 && c.ICPScore < 0.7)}</li>
        <li><strong>Generated:</strong> {DateTime.Now:yyyy-MM-dd HH:mm}</li>
    </ul>

    <h2>🏢 Company Details</h2>";

            foreach (var company in companies.OrderByDescending(c => c.ICPScore))
            {
                var scoreClass = company.ICPScore >= 0.7 ? "high-score" : 
                               company.ICPScore >= 0.4 ? "medium-score" : "low-score";
                
                html += $@"
    <div class='company {scoreClass}'>
        <h3>{company.Name}</h3>
        <div class='score'>ICP Score: {company.ICPScore:F2}</div>
        <div class='details'>
            <p><strong>Sector:</strong> {company.Sector}</p>
            <p class='funding'><strong>Funding:</strong> {company.Round} - {company.Amount}</p>
            <p class='location'><strong>Location:</strong> {company.Headquarters}</p>
            <p><strong>Description:</strong> {company.Snippet}</p>
        </div>
    </div>";
            }

            html += @"
    <div class='footer'>
        <p>📎 Detailed results are attached as JSON file for further processing.</p>
        <p>🤖 Generated by Hikru Lead Research Agent - Team A Hackathon</p>
    </div>
</body>
</html>";

            return html;
        }

        private string GenerateCompanyReportAttachment(List<Company> companies)
        {
            var report = new
            {
                GeneratedAt = DateTime.Now,
                TotalCompanies = companies.Count,
                HighScoreCompanies = companies.Count(c => c.ICPScore >= 0.7),
                Companies = companies.OrderByDescending(c => c.ICPScore).Select(c => new
                {
                    c.Name,
                    c.Sector,
                    c.Round,
                    c.Amount,
                    c.Headquarters,
                    c.Snippet,
                    ICPScore = Math.Round(c.ICPScore, 3)
                })
            };

            return Newtonsoft.Json.JsonConvert.SerializeObject(report, Newtonsoft.Json.Formatting.Indented);
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