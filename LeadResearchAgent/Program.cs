using Microsoft.SemanticKernel;
using Microsoft.Graph;
using Azure.Identity;
using LeadResearchAgent.Agents;
using LeadResearchAgent.Services;
using Microsoft.SemanticKernel.ChatCompletion;

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
                
                // Test kernel connection
                Console.WriteLine("\n🧪 Testing Kernel connection...");
                var isConnected = await TestKernelConnectionAsync(kernel);
                
                if (!isConnected)
                {
                    Console.WriteLine("⚠️  Kernel is in demo mode. Some AI features may not work.");
                }
                else
                {
                    Console.WriteLine("✅ Kernel connection verified!");
                }
                
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

        private static async Task<bool> TestKernelConnectionAsync(Kernel kernel)
        {
            try
            {
                // Check if kernel has any chat completion services
                var chatCompletionServices = kernel.GetAllServices<IChatCompletionService>();
                
                if (!chatCompletionServices.Any())
                {
                    Console.WriteLine("⚠️  No chat completion services found in kernel.");
                    return false;
                }

                var chatService = chatCompletionServices.First();
                Console.WriteLine($"📡 Found chat service: {chatService.GetType().Name}");

                // Try a simple test prompt
                Console.WriteLine("🔄 Sending test message to Azure OpenAI...");
                var userMessage = "Respond with exactly 'Kernel connection successful' and nothing else.";
                var response = await chatService.GetChatMessageContentAsync(
                    userMessage,
                    new PromptExecutionSettings { ExtensionData = new Dictionary<string, object> { ["max_tokens"] = 50 } }
                );

                if (response != null && !string.IsNullOrEmpty(response.Content))
                {
                    Console.WriteLine($"✅ Response received: {response.Content}");
                    return true;
                }

                return false;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"❌ Network/API error: {ex.Message}");
                Console.WriteLine("   Check your AZURE_OPENAI_ENDPOINT and network connectivity.");
                return false;
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine($"❌ Configuration error: {ex.Message}");
                Console.WriteLine("   Check your AZURE_OPENAI_API_KEY and AZURE_OPENAI_DEPLOYMENT.");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Kernel test failed: {ex.GetType().Name}: {ex.Message}");
                return false;
            }
        }
    }
}
