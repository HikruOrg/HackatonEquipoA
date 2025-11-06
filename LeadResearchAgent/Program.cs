using Microsoft.SemanticKernel;
using Microsoft.Graph;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Http;
using LeadResearchAgent.Agents;
using LeadResearchAgent.Services;
using LeadResearchAgent.Models;

namespace LeadResearchAgent
{
    class Program
    {
        private static AzureConfigurationService? _configService;
        private static IServiceProvider? _serviceProvider;

        static async Task Main(string[] args)
        {
            Console.WriteLine("🚀 Lead Research Agent - Hikru Hackathon Team A");
            Console.WriteLine("📧 LinkSV Pulse Newsletter Processor");
            Console.WriteLine("================================================");

            try
            {
                // Initialize configuration and services
                await InitializeServicesAsync();

                if (_configService == null || _serviceProvider == null)
                {
                    Console.WriteLine("❌ Failed to initialize services");
                    return;
                }

                // Get Azure credentials
                var credentials = await _configService.GetAzureCredentialsAsync();
                
                // Initialize Semantic Kernel with Azure OpenAI
                var kernel = await InitializeKernelAsync(credentials);
                
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
                var agent = new LeadResearchAgent.Agents.LeadResearchAgent(kernel, icpPath, enrichmentPath);

                // Check if user wants to process Outlook emails or file
                Console.WriteLine("\n📋 Choose your newsletter source:");
                Console.WriteLine("1. Process LinkSV Pulse emails from Outlook (requires authentication)");
                Console.WriteLine("2. Process sample newsletter file (demo mode)");
                Console.Write("Enter your choice (1 or 2): ");
                
                var choice = Console.ReadLine();

                if (choice == "1")
                {
                    await ProcessOutlookEmailsAsync(agent, credentials);
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

        private static async Task InitializeServicesAsync()
        {
            // Build configuration
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            // Build service provider
            var services = new ServiceCollection();
            
            // Add logging
            services.AddLogging(builder =>
            {
                builder.AddConfiguration(configuration.GetSection("Logging"));
                builder.AddConsole();
            });

            // Add configuration service
            services.AddSingleton<IConfiguration>(configuration);
            services.AddSingleton<AzureConfigurationService>();
            
            // Add HTTP client for AI Foundry
            services.AddHttpClient<AzureAIFoundryService>();
            
            _serviceProvider = services.BuildServiceProvider();
            _configService = _serviceProvider.GetRequiredService<AzureConfigurationService>();
            
            Console.WriteLine("✅ Services initialized successfully");
        }

        private static async Task ProcessOutlookEmailsAsync(LeadResearchAgent.Agents.LeadResearchAgent agent, AzureCredentials credentials)
        {
            try
            {
                Console.WriteLine("\n🔐 Initializing Microsoft Graph client...");
                var graphClient = await InitializeGraphClientAsync(credentials);
                var logger = _serviceProvider?.GetService<ILogger<OutlookEmailService>>();
                var outlookService = new OutlookEmailService(graphClient, logger);

                // Initialize AI Foundry service if available
                AzureAIFoundryService? aiFoundryService = null;
                if (credentials.IsAIFoundryConfigured)
                {
                    var httpClient = _serviceProvider?.GetRequiredService<HttpClient>();
                    var aiLogger = _serviceProvider?.GetService<ILogger<AzureAIFoundryService>>();
                    if (httpClient != null)
                    {
                        aiFoundryService = new AzureAIFoundryService(
                            httpClient, 
                            credentials.AIFoundryEndpoint, 
                            credentials.AIFoundryApiKey, 
                            credentials.AIFoundryProjectId, 
                            aiLogger);
                        Console.WriteLine("✅ Azure AI Foundry service initialized");
                    }
                }

                Console.WriteLine("\n📬 Fetching LinkSV Pulse emails...");
                var emailContents = await outlookService.GetLinkSVPulseEmailsAsync(maxEmails: 5);

                if (!emailContents.Any())
                {
                    Console.WriteLine("⚠️ No LinkSV Pulse emails found. Make sure you have emails in your inbox.");
                    return;
                }

                Console.WriteLine($"📧 Found {emailContents.Count} emails to process");

                // Process each email
                var allResults = new List<Company>();
                
                foreach (var (emailContent, index) in emailContents.Select((content, i) => (content, i)))
                {
                    Console.WriteLine($"\n📖 Processing email {index + 1}/{emailContents.Count}...");
                    var results = await agent.ProcessNewsletterAsync(emailContent, minimumICPScore: 0.3);
                    
                    // Enhance with AI Foundry if available
                    if (aiFoundryService?.IsConfigured == true && results.Any())
                    {
                        Console.WriteLine("🤖 Enhancing results with Azure AI Foundry...");
                        var icp = agent.LoadICP();
                        
                        for (int i = 0; i < results.Count; i++)
                        {
                            // Enhance ICP scoring
                            var enhancedScore = await aiFoundryService.EnhanceICPScoringAsync(results[i], icp);
                            results[i].ICPScore = enhancedScore;
                            
                            // Enrich company data
                            results[i] = await aiFoundryService.EnrichCompanyDataAsync(results[i]);
                        }
                    }
                    
                    allResults.AddRange(results);
                    Console.WriteLine($"✅ Found {results.Count} companies in email {index + 1}");
                }

                // Remove duplicates and sort by score
                var uniqueResults = allResults
                    .GroupBy(c => c.Name.ToLower())
                    .Select(g => g.OrderByDescending(c => c.ICPScore).First())
                    .OrderByDescending(c => c.ICPScore)
                    .ToList();

                // Display combined results
                agent.PrintResults(uniqueResults);

                // Analyze market trends with AI Foundry
                if (aiFoundryService?.IsConfigured == true && uniqueResults.Any())
                {
                    Console.WriteLine("\n📊 Analyzing market trends...");
                    var insights = await aiFoundryService.AnalyzeMarketTrendsAsync(uniqueResults);
                    Console.WriteLine($"Market Insights: {insights.Summary}");
                    if (insights.Recommendations.Any())
                    {
                        Console.WriteLine("📋 Recommendations:");
                        foreach (var rec in insights.Recommendations)
                        {
                            Console.WriteLine($"  • {rec}");
                        }
                    }
                }

                // Export results
                if (uniqueResults.Any())
                {
                    var outputPath = Path.Combine("Data", $"linksv_results_{DateTime.Now:yyyyMMdd_HHmmss}.json");
                    await agent.ExportToJsonAsync(uniqueResults, outputPath);

                    // Send email summary if configured
                    if (!string.IsNullOrEmpty(credentials.DefaultRecipientEmail))
                    {
                        Console.WriteLine($"\n📤 Sending results to {credentials.DefaultRecipientEmail}...");
                        var emailSent = await outlookService.SendLeadSummaryEmailAsync(
                            credentials.DefaultRecipientEmail, 
                            uniqueResults,
                            $"LinkSV Pulse Analysis - {emailContents.Count} emails processed");
                        
                        if (emailSent)
                        {
                            Console.WriteLine("✅ Results sent successfully!");
                        }
                        else
                        {
                            Console.WriteLine("⚠️ Failed to send email - check permissions");
                        }
                    }
                    else
                    {
                        Console.WriteLine("\n💡 Set DEFAULT_RECIPIENT_EMAIL to automatically send results via email");
                    }
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

        private static async Task ProcessNewsletterFileAsync(LeadResearchAgent.Agents.LeadResearchAgent agent, string newsletterPath)
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

        private static async Task<Kernel> InitializeKernelAsync(AzureCredentials credentials)
        {
            Console.WriteLine("🔧 Initializing Semantic Kernel...");

            if (!credentials.IsOpenAIConfigured)
            {
                Console.WriteLine("⚠️  Azure OpenAI configuration not found.");
                Console.WriteLine("For demo purposes, creating a mock kernel...");
                Console.WriteLine("ℹ️  To use real AI, configure Azure OpenAI credentials");

                return Kernel.CreateBuilder().Build();
            }

            try
            {
                var kernelBuilder = Kernel.CreateBuilder();
                kernelBuilder.AddAzureOpenAIChatCompletion(
                    deploymentName: credentials.OpenAIDeployment ?? "gpt-4",
                    endpoint: credentials.OpenAIEndpoint!,
                    apiKey: credentials.OpenAIApiKey!);

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

        private static async Task<GraphServiceClient> InitializeGraphClientAsync(AzureCredentials credentials)
        {
            Console.WriteLine("🔐 Initializing Microsoft Graph authentication...");
            
            // Check for automation preference from environment
            var useDeviceCode = Environment.GetEnvironmentVariable("USE_DEVICE_CODE_AUTH")?.ToLower() == "true";
            
            if (credentials.IsGraphConfigured && !string.IsNullOrEmpty(credentials.GraphClientSecret))
            {
                // Use client secret flow for service-to-service scenarios
                // Note: This requires application permissions and accessing a specific user's mailbox
                Console.WriteLine("✅ Using automated authentication (client credentials flow)...");
                Console.WriteLine("💡 This allows the application to run without browser interaction");
                Console.WriteLine("⚠️  Note: Client credentials flow requires Mail.ReadWrite application permission");
                Console.WriteLine("⚠️  and accessing a specific user mailbox (e.g., Users('user@domain.com'))");
                
                var options = new ClientSecretCredentialOptions
                {
                    AuthorityHost = AzureAuthorityHosts.AzurePublicCloud,
                };

                var credential = new ClientSecretCredential(
                    credentials.GraphTenantId,
                    credentials.GraphClientId,
                    credentials.GraphClientSecret,
                    options);

                var scopes = new[] { "https://graph.microsoft.com/.default" };
                return new GraphServiceClient(credential, scopes);
            }
            else if (credentials.IsGraphConfigured && useDeviceCode)
            {
                // Use device code flow for automation without browser
                Console.WriteLine("📱 Using device code authentication (no browser required)...");
                Console.WriteLine("💡 You'll receive a code to enter at https://microsoft.com/devicelogin");
                Console.WriteLine("💡 This is ideal for automation or remote systems");
                
                var options = new DeviceCodeCredentialOptions
                {
                    TenantId = credentials.GraphTenantId,
                    ClientId = credentials.GraphClientId,
                    AuthorityHost = AzureAuthorityHosts.AzurePublicCloud,
                    DeviceCodeCallback = (code, cancellation) =>
                    {
                        Console.WriteLine($"\n{'=',-60}");
                        Console.WriteLine("🔐 DEVICE CODE AUTHENTICATION");
                        Console.WriteLine($"{'=',-60}");
                        Console.WriteLine($"📱 User Code: {code.UserCode}");
                        Console.WriteLine($"🌐 Verification URL: {code.VerificationUri}");
                        Console.WriteLine($"⏱️  Expires: {code.ExpiresOn}");
                        Console.WriteLine($"{'=',-60}");
                        Console.WriteLine("\n✅ Please visit the URL above and enter the code to authenticate.");
                        Console.WriteLine("⏳ Waiting for authentication...\n");
                        return Task.CompletedTask;
                    },
                };

                var credential = new DeviceCodeCredential(options);
                var scopes = new[] { "Mail.Read", "Mail.Send", "User.Read" };
                return new GraphServiceClient(credential, scopes);
            }
            else if (credentials.IsGraphConfigured)
            {
                // Use interactive browser authentication with your app registration
                Console.WriteLine("🌐 Using interactive browser authentication...");
                Console.WriteLine("💡 A browser window will open for sign-in");
                Console.WriteLine("\n📝 Alternative authentication options:");
                Console.WriteLine("   • For automation with device code: Set USE_DEVICE_CODE_AUTH=true");
                Console.WriteLine("   • For service automation: Configure MICROSOFT_GRAPH_CLIENT_SECRET");
                
                try
                {
                    var options = new InteractiveBrowserCredentialOptions
                    {
                        TenantId = credentials.GraphTenantId,
                        ClientId = credentials.GraphClientId,
                        AuthorityHost = AzureAuthorityHosts.AzurePublicCloud,
                        RedirectUri = new Uri("http://localhost"),
                    };

                    var credential = new InteractiveBrowserCredential(options);
                    var scopes = new[] { "Mail.Read", "Mail.Send", "User.Read" };
                    return new GraphServiceClient(credential, scopes);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"\n❌ Interactive authentication failed: {ex.Message}");
                    Console.WriteLine("\n💡 Troubleshooting tips:");
                    Console.WriteLine("   1. Ensure redirect URI 'http://localhost' is configured in Azure Portal");
                    Console.WriteLine("   2. Check that your app has delegated permissions: Mail.Read, Mail.Send, User.Read");
                    Console.WriteLine("   3. Try device code authentication: Set USE_DEVICE_CODE_AUTH=true");
                    Console.WriteLine("   4. Verify your Client ID and Tenant ID are correct");
                    throw;
                }
            }
            else
            {
                // Fallback to Microsoft Graph PowerShell public client
                Console.WriteLine("⚠️  Graph configuration incomplete - using fallback authentication");
                Console.WriteLine("💡 For better control, configure your own app registration:");
                Console.WriteLine("   1. Create an app registration in Azure Portal");
                Console.WriteLine("   2. Add redirect URI: http://localhost (type: Public client/native)");
                Console.WriteLine("   3. Grant delegated permissions: Mail.Read, Mail.Send, User.Read");
                Console.WriteLine("   4. Set environment variables:");
                Console.WriteLine("      • MICROSOFT_GRAPH_CLIENT_ID");
                Console.WriteLine("      • MICROSOFT_GRAPH_TENANT_ID");
                Console.WriteLine("🌐 Opening browser for authentication...");
                
                var options = new InteractiveBrowserCredentialOptions
                {
                    TenantId = credentials.GraphTenantId,
                    ClientId = "14d82eec-204b-4c2f-b7e8-296a70dab67e", // Microsoft Graph PowerShell
                    AuthorityHost = AzureAuthorityHosts.AzurePublicCloud,
                    RedirectUri = new Uri("http://localhost"),
                };

                var credential = new InteractiveBrowserCredential(options);
                var scopes = new[] { "Mail.Read", "Mail.Send", "User.Read" };
                return new GraphServiceClient(credential, scopes);
            }
        }
    }
}