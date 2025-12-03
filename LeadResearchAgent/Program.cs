using Azure.Identity;
using LeadResearchAgent.Agents;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Graph;

namespace LeadResearchAgent
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var host = Host.CreateDefaultBuilder(args)
                .ConfigureServices((context, services) =>
                {
                    // Register GraphServiceClient
                    services.AddSingleton<GraphServiceClient>(sp =>
                    {
                        return InitializeGraphClient();
                    });

                    // Register AzureFoundryLeadAgent
                    services.AddSingleton<AzureFoundryLeadAgent>();

                    // Register the Worker as a hosted service
                    services.AddHostedService<Worker>();
                })
                .Build();

            await host.RunAsync();
        }

        private static GraphServiceClient InitializeGraphClient()
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

                var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
                return new GraphServiceClient(credential);
            }
        }
    }
}
