using Azure.Identity;
using LeadResearchAgent.Agents;
using LeadResearchAgent.Models;
using LeadResearchAgent.Services;
using Microsoft.Graph;

namespace LeadResearchAgent
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Lead Research Agent - starting");

            try
            {
                // Use Azure Foundry agent for analysis
                var foundryAgent = new AzureFoundryLeadAgent();

                var emails = await LoadOutlookEmailsAsync();

                await ProcessNewsletterWithFoundryAsync(foundryAgent, emails?.FirstOrDefault());

                Console.WriteLine("Processing completed.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        private static async Task ProcessNewsletterWithFoundryAsync(AzureFoundryLeadAgent foundryAgent, string email)
        {

            var results = await foundryAgent.ProcessNewsletterAsync(email);

            await SendResultsByEmailAsync("daniel.ramirez@hikrutech.com", results);

        }

        private static async Task<List<string>> LoadOutlookEmailsAsync()
        {
            try
            {
                Console.WriteLine("\n📧 Connecting to Outlook...");

                // Initialize Graph client
                var graphClient = await InitializeGraphClientAsync();

                // Get the user ID - use "me" for delegated auth, or get from config for app-only
                var userId = Environment.GetEnvironmentVariable("MICROSOFT_GRAPH_USER_ID") ?? "me";
                var outlookService = new OutlookEmailService(graphClient, userId);

                // Get recent LinkSV Pulse emails
                Console.WriteLine("🔍 Searching for LinkSV Pulse newsletters...");
                var emails = await outlookService.GetLinkSVPulseEmailsAsync(10);

                if (!emails.Any())
                {
                    Console.WriteLine("❌ No LinkSV Pulse emails found. Please check:");
                    Console.WriteLine("   - You have LinkSV Pulse emails in your inbox");
                    Console.WriteLine("   - Your Microsoft Graph permissions are correctly configured");
                    return new List<string>();
                }

                Console.WriteLine($"✅ Found {emails.Count} LinkSV Pulse newsletters");

                return emails;

            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Failed to process Outlook emails: {ex.Message}");
                Console.WriteLine("💡 Falling back to demo mode with sample newsletter...");
                return new List<string>();
            }
        }

        private static async Task<GraphServiceClient> InitializeGraphClientAsync()
        {
            var clientId = Environment.GetEnvironmentVariable("MICROSOFT_GRAPH_CLIENT_ID");
            var tenantId = Environment.GetEnvironmentVariable("MICROSOFT_GRAPH_TENANT_ID");
            var clientSecret = Environment.GetEnvironmentVariable("MICROSOFT_GRAPH_CLIENT_SECRET");

            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(tenantId) || string.IsNullOrEmpty(clientSecret))
            {
                Console.WriteLine("⚠️  Client credentials not configured. Using interactive authentication...");

                var options = new InteractiveBrowserCredentialOptions
                {
                    TenantId = tenantId,
                    ClientId = "14d82eec-204b-4c2f-b7e8-296a70dab67e",
                    AuthorityHost = AzureAuthorityHosts.AzurePublicCloud,
                    RedirectUri = new Uri("http://localhost"),
                };

                var credential = new InteractiveBrowserCredential(options);
                return new GraphServiceClient(credential);
            }
            else
            {
                Console.WriteLine("🔐 Using app registration (client credentials) - Production mode...");

                // Use ClientSecretCredential for production/unattended authentication
                var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
                return new GraphServiceClient(credential);
            }
        }

        private static async Task SendResultsByEmailAsync(string recipientEmail, List<FoundryCompanyResult> results)
        {
            // Initialize Graph client
            var graphClient = await InitializeGraphClientAsync();

            // Construye el HTML del correo
            var htmlBuilder = new System.Text.StringBuilder();
            htmlBuilder.AppendLine("<h2>Resultados Lead Research Agent</h2>");
            htmlBuilder.AppendLine("<ul>");
            foreach (var r in results)
            {
                htmlBuilder.AppendLine("<li>");
                htmlBuilder.AppendLine($"<strong>{r.Empresa?.Nombre}</strong> ({r.Empresa?.Sector}, {r.Empresa?.Pais})<br>");
                htmlBuilder.AppendLine($"<b>Capital:</b> {r.TotalCapital}<br>");
                htmlBuilder.AppendLine($"<b>Interés:</b> {r.NivelInteres}<br>");
                htmlBuilder.AppendLine($"<b>Resumen:</b> {r.Resumen}<br>");
                htmlBuilder.AppendLine($"<b>Razón de match:</b> {r.RazonDeMatch}<br>");
                htmlBuilder.AppendLine("</li>");
                htmlBuilder.AppendLine("</br>");
            }
            htmlBuilder.AppendLine("</ul>");

            var message = new Microsoft.Graph.Models.Message
            {
                Subject = "Resultados Lead Research Agent",
                Body = new Microsoft.Graph.Models.ItemBody
                {
                    ContentType = Microsoft.Graph.Models.BodyType.Html,
                    Content = htmlBuilder.ToString()
                },
                ToRecipients = new List<Microsoft.Graph.Models.Recipient>
                {
                    new Microsoft.Graph.Models.Recipient
                    {
                        EmailAddress = new Microsoft.Graph.Models.EmailAddress
                        {
                            Address = recipientEmail
                        }
                    }
                }
            };
            try
            {
                var userId = Environment.GetEnvironmentVariable("MICROSOFT_GRAPH_USER_ID") ?? "me";
                await graphClient.Users[userId].SendMail.PostAsync(new Microsoft.Graph.Users.Item.SendMail.SendMailPostRequestBody
                {
                    Message = message,
                    SaveToSentItems = true
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending email: {ex.Message}");
                throw;
            }


        }
    }
}
