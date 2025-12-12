using LeadResearchAgent.Models;
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
        public async Task<List<EmailMessage>> GetLinkSVPulseEmailsAsync(int maxEmails = 10)
        {
            try
            {
                _logger?.LogInformation("Fetching unread LinkSV Pulse emails from Outlook...");

                // Search for unread emails from LinkSV Pulse
                var messages = await _graphServiceClient.Users[_userId].Messages
                    .GetAsync(requestConfiguration =>
                    {
                        requestConfiguration.QueryParameters.Filter = "isRead eq false and startsWith(subject, 'Fw: Pulse of the Valley Premium')";
                        requestConfiguration.QueryParameters.Top = maxEmails;
                        //requestConfiguration.QueryParameters.Orderby = new[] { "receivedDateTime desc" };
                        //requestConfiguration.QueryParameters.Select = new[] { "subject", "body", "from", "receivedDateTime", "isRead" };
                    });

                var emailMessages = new List<EmailMessage>();

                if (messages?.Value != null)
                {
                    foreach (var message in messages.Value)
                    {
                        if (message.Body?.Content != null && message.Id != null)
                        {
                            //var emailContent = ExtractTextFromHtml(message.Body.Content);
                            var emailContent = message.Body.Content;
                            emailMessages.Add(new EmailMessage
                            {
                                Id = message.Id,
                                Content = emailContent
                            });
                            
                            _logger?.LogInformation($"Retrieved unread email: {message.Subject} from {message.From?.EmailAddress?.Address}");
                        }
                    }
                }

                _logger?.LogInformation($"Successfully retrieved {emailMessages.Count} unread LinkSV Pulse emails");
                return emailMessages;
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

        /// <summary>
        /// Sends the analysis results by email
        /// </summary>
        public async Task SendResultsEmailAsync(string recipientEmail, List<FoundryCompanyResult> results, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger?.LogInformation("Preparing results email for {Email}", recipientEmail);

                var htmlContent = BuildResultsHtmlContent(results);

                var message = new Message
                {
                    Subject = "Resultados Lead Research Agent",
                    Body = new ItemBody
                    {
                        ContentType = BodyType.Html,
                        Content = htmlContent
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

                await _graphServiceClient.Users[_userId].SendMail.PostAsync(
                    new Microsoft.Graph.Users.Item.SendMail.SendMailPostRequestBody
                    {
                        Message = message,
                        SaveToSentItems = true
                    }, 
                    cancellationToken: cancellationToken);

                _logger?.LogInformation("✅ Results email sent successfully to {Email}", recipientEmail);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error sending results email: {Message}", ex.Message);
                throw;
            }
        }

        private string BuildResultsHtmlContent(List<FoundryCompanyResult> results)
        {
            // Separate accepted and rejected results
            var aceptadas = results
                .Where(r => r.NivelInteres != null &&
                            (r.NivelInteres.Equals("alto", StringComparison.OrdinalIgnoreCase) ||
                             r.NivelInteres.Equals("medio", StringComparison.OrdinalIgnoreCase) ||
                             r.NivelInteres.Equals("bajo", StringComparison.OrdinalIgnoreCase)))
                .ToList();

            var descartadas = results
                .Where(r => r.NivelInteres != null &&
                            r.NivelInteres.Equals("descartar", StringComparison.OrdinalIgnoreCase))
                .ToList();

            var htmlBuilder = new System.Text.StringBuilder();
            htmlBuilder.AppendLine("<h2>Resultados Lead Research Agent</h2>");

            // Accepted section
            htmlBuilder.AppendLine("<h3>Aceptadas</h3>");
            htmlBuilder.AppendLine("<ul>");
            foreach (var r in aceptadas)
            {
                htmlBuilder.AppendLine("<li>");
                var companyName = r.Empresa?.Nombre ?? "N/A";
                if (!string.IsNullOrEmpty(r.EmpresaUrl))
                {
                    htmlBuilder.AppendLine($"<strong><a href=\"{r.EmpresaUrl}\" target=\"_blank\">{companyName}</a></strong> ({r.Empresa?.Sector}, {r.Empresa?.Pais})<br>");
                }
                else
                {
                    htmlBuilder.AppendLine($"<strong>{companyName}</strong> ({r.Empresa?.Sector}, {r.Empresa?.Pais})<br>");
                }
                htmlBuilder.AppendLine($"<b>Capital:</b> {r.TotalCapital}M <br>");
                htmlBuilder.AppendLine($"<b>Interés:</b> {r.NivelInteres}<br>");
                htmlBuilder.AppendLine($"<b>Resumen:</b> {r.Resumen}<br>");
                htmlBuilder.AppendLine($"<b>Razón de match:</b> {r.RazonDeMatch}<br>");
                htmlBuilder.AppendLine("</li>");
                htmlBuilder.AppendLine("</br>");
            }
            htmlBuilder.AppendLine("</ul>");

            if (descartadas.Any())
            {
                // Rejected section
                htmlBuilder.AppendLine("<h3>Descartadas</h3>");
                htmlBuilder.AppendLine("<ul>");
                foreach (var r in descartadas)
                {
                    htmlBuilder.AppendLine("<li>");
                    htmlBuilder.AppendLine($"<strong>{r.Empresa?.Nombre}</strong> ({r.Empresa?.Sector}, {r.Empresa?.Pais})<br>");
                    htmlBuilder.AppendLine($"<b>Capital:</b> {r.TotalCapital}M <br>");
                    htmlBuilder.AppendLine($"<b>Interés:</b> {r.NivelInteres}<br>");
                    htmlBuilder.AppendLine($"<b>Resumen:</b> {r.Resumen}<br>");
                    htmlBuilder.AppendLine($"<b>Razón de descarte:</b> {r.RazonDeMatch}<br>");
                    htmlBuilder.AppendLine("</li>");
                    htmlBuilder.AppendLine("</br>");
                }
                htmlBuilder.AppendLine("</ul>");
            }

            return htmlBuilder.ToString();
        }
    }
}