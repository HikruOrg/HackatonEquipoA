using Microsoft.SemanticKernel;
using Microsoft.Graph;
using Azure.Identity;
using LeadResearchAgent.Agents;
using LeadResearchAgent.Services;

namespace LeadResearchAgent
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("🚀 Lead Research Agent - Hikru Hackathon Team A");
            Console.WriteLine("📧 LinkSV Pulse Newsletter Processor");
            Console.WriteLine("================================================");

            try
            {
                // Initialize Semantic Kernel with Azure OpenAI
                var kernel = await InitializeKernelAsync();
                
                // Paths to configuration files
                var icpPath = Path.Combine("Data", "icp.json");
                var enrichmentPath = Path.Combine("Data", "enrichment.csv");
                var newsletterPath = Path.Combine("Data", "sample_newsletter.txt");

                // Verify ICP file exists
                if (!File.Exists(icpPath))
                {
                    Console.WriteLine($"❌ ICP file not found: {icpPath}");
                    Console.WriteLine("Please create the ICP.json file in the Data folder.");
                    return;
                }

                // Initialize Lead Research Agent
                var agent = new Agents.LeadResearchAgent(kernel, icpPath, enrichmentPath);

                // Check if user wants to process Outlook emails or file
                Console.WriteLine("\n📋 Choose your newsletter source:");
                Console.WriteLine("1. Process LinkSV Pulse emails from Outlook (requires authentication)");
                Console.WriteLine("2. Process sample newsletter file (demo mode)");
                Console.Write("Enter your choice (1 or 2): ");
                
                var choice = Console.ReadLine();

                if (choice == "1")
                {
                    await ProcessOutlookEmailsAsync(agent);
                }
                else
                {
                    await ProcessNewsletterFileAsync(agent, newsletterPath);
                }

                Console.WriteLine("\n✅ Lead Research Agent processing completed!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }

        private static async Task ProcessOutlookEmailsAsync(Agents.LeadResearchAgent agent)
        {
            try
            {
                Console.WriteLine("\n📧 Connecting to Outlook...");
                
                // Initialize Graph client
                var graphClient = await InitializeGraphClientAsync();
                var outlookService = new OutlookEmailService(graphClient);

                // Get recent LinkSV Pulse emails
                Console.WriteLine("🔍 Searching for LinkSV Pulse newsletters...");
                var emails = await outlookService.GetLinkSVPulseEmailsAsync(10);

                if (!emails.Any())
                {
                    Console.WriteLine("❌ No LinkSV Pulse emails found. Please check:");
                    Console.WriteLine("   - You have LinkSV Pulse emails in your inbox");
                    Console.WriteLine("   - Your Microsoft Graph permissions are correctly configured");
                    return;
                }

                Console.WriteLine($"✅ Found {emails.Count} LinkSV Pulse newsletters");

                // Process each email
                var allResults = new List<LeadResearchAgent.Models.Company>();
                for (int i = 0; i < emails.Count; i++)
                {
                    Console.WriteLine($"\n📧 Processing email {i + 1}/{emails.Count}...");
                    var results = await agent.ProcessNewsletterAsync(emails[i], minimumICPScore: 0.3);
                    allResults.AddRange(results);
                }

                // Remove duplicates and sort by score
                var uniqueResults = allResults
                    .GroupBy(c => c.Name.ToLower())
                    .Select(g => g.OrderByDescending(c => c.ICPScore).First())
                    .OrderByDescending(c => c.ICPScore)
                    .ToList();

                // Display combined results
                agent.PrintResults(uniqueResults);

                // Export results
                if (uniqueResults.Any())
                {
                    var outputPath = Path.Combine("Data", $"linksv_results_{DateTime.Now:yyyyMMdd_HHmmss}.json");
                    await agent.ExportToJsonAsync(uniqueResults, outputPath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Failed to process Outlook emails: {ex.Message}");
                Console.WriteLine("💡 Falling back to demo mode with sample newsletter...");
                
                var newsletterPath = Path.Combine("Data", "sample_newsletter.txt");
                await ProcessNewsletterFileAsync(agent, newsletterPath);
            }
        }

        private static async Task ProcessNewsletterFileAsync(Agents.LeadResearchAgent agent, string newsletterPath)
        {
            if (!File.Exists(newsletterPath))
            {
                Console.WriteLine($"❌ Newsletter file not found: {newsletterPath}");
                Console.WriteLine("Please provide a newsletter text file.");
                return;
            }

            // Read newsletter content
            var newsletterText = await File.ReadAllTextAsync(newsletterPath);
            Console.WriteLine($"📖 Loaded newsletter: {newsletterText.Length} characters");

            // Process the newsletter
            var results = await agent.ProcessNewsletterAsync(newsletterText, minimumICPScore: 0.3);

            // Display results
            agent.PrintResults(results);

            // Export results
            if (results.Any())
            {
                var outputPath = Path.Combine("Data", $"results_{DateTime.Now:yyyyMMdd_HHmmss}.json");
                await agent.ExportToJsonAsync(results, outputPath);
            }
        }

        private static async Task<Kernel> InitializeKernelAsync()
        {
            Console.WriteLine("🔧 Initializing Semantic Kernel...");

            var apiKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY");
            var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
            var deploymentName = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT") ?? "gpt-4";

            if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(endpoint))
            {
                Console.WriteLine("⚠️  Azure OpenAI configuration not found in environment variables.");
                Console.WriteLine("For demo purposes, creating a mock kernel...");
                Console.WriteLine("ℹ️  To use real AI, set environment variables:");
                Console.WriteLine("   - AZURE_OPENAI_API_KEY");
                Console.WriteLine("   - AZURE_OPENAI_ENDPOINT");
                Console.WriteLine("   - AZURE_OPENAI_DEPLOYMENT (optional, defaults to gpt-4)");

                return Kernel.CreateBuilder().Build();
            }

            try
            {
                var kernelBuilder = Kernel.CreateBuilder();
                kernelBuilder.AddAzureOpenAIChatCompletion(
                    deploymentName: deploymentName,
                    endpoint: endpoint,
                    apiKey: apiKey);

                var kernel = kernelBuilder.Build();
                Console.WriteLine("✅ Semantic Kernel initialized with Azure OpenAI");
                return kernel;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Failed to initialize Azure OpenAI: {ex.Message}");
                Console.WriteLine("Creating basic kernel for demo...");
                return Kernel.CreateBuilder().Build();
            }
        }

        private static async Task<GraphServiceClient> InitializeGraphClientAsync()
        {
            var clientId = Environment.GetEnvironmentVariable("MICROSOFT_GRAPH_CLIENT_ID");
            var tenantId = Environment.GetEnvironmentVariable("MICROSOFT_GRAPH_TENANT_ID");

            if (string.IsNullOrEmpty(clientId))
            {
                Console.WriteLine("⚠️  Using interactive authentication (browser will open)...");
                
                // Interactive authentication for demo
                var options = new InteractiveBrowserCredentialOptions
                {
                    TenantId = tenantId,
                    ClientId = "14d82eec-204b-4c2f-b7e8-296a70dab67e", // Microsoft Graph PowerShell public client
                    AuthorityHost = AzureAuthorityHosts.AzurePublicCloud,
                    RedirectUri = new Uri("http://localhost"),
                };

                var credential = new InteractiveBrowserCredential(options);
                var graphClient = new GraphServiceClient(credential);
                
                return graphClient;
            }
            else
            {
                // App registration authentication
                var options = new DeviceCodeCredentialOptions
                {
                    AuthorityHost = AzureAuthorityHosts.AzurePublicCloud,
                    ClientId = clientId,
                    TenantId = tenantId,
                };

                var credential = new DeviceCodeCredential(options);
                var graphClient = new GraphServiceClient(credential);
                
                return graphClient;
            }
        }
    }
}
