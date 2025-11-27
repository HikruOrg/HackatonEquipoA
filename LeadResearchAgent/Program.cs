using Azure.Identity;
using LeadResearchAgent.Agents;
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

            // Save raw JSON to file
            var outputPath = Path.Combine("Data", $"foundry_results_{DateTime.Now:yyyyMMdd_HHmmss}.json");
            var json = System.Text.Json.JsonSerializer.Serialize(results, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(outputPath, json);
            Console.WriteLine($"Results saved to: {outputPath} (items: {results.Count})");
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
    }
}
