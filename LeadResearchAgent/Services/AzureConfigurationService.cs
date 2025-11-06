using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;

namespace LeadResearchAgent.Services
{
    public class AzureConfigurationService
    {
        private readonly IConfiguration _configuration;
        private readonly SecretClient? _keyVaultClient;

        public AzureConfigurationService(IConfiguration configuration)
        {
            _configuration = configuration;
            
            // Initialize Key Vault client if available
            var keyVaultUrl = _configuration["Azure:KeyVault:Url"];
            if (!string.IsNullOrEmpty(keyVaultUrl))
            {
                try
                {
                    var credential = new DefaultAzureCredential();
                    _keyVaultClient = new SecretClient(new Uri(keyVaultUrl), credential);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Key Vault initialization failed: {ex.Message}");
                }
            }
        }

        public async Task<string?> GetSecretAsync(string secretName, string? fallbackConfigKey = null)
        {
            // Try Key Vault first
            if (_keyVaultClient != null)
            {
                try
                {
                    var secret = await _keyVaultClient.GetSecretAsync(secretName);
                    if (!string.IsNullOrEmpty(secret.Value.Value))
                    {
                        return secret.Value.Value;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Failed to retrieve secret '{secretName}' from Key Vault: {ex.Message}");
                }
            }

            // Fallback to configuration/environment variables
            if (!string.IsNullOrEmpty(fallbackConfigKey))
            {
                return _configuration[fallbackConfigKey];
            }

            return Environment.GetEnvironmentVariable(secretName);
        }

        public async Task<AzureCredentials> GetAzureCredentialsAsync()
        {
            return new AzureCredentials
            {
                // Azure OpenAI
                OpenAIApiKey = await GetSecretAsync("AZURE-OPENAI-API-KEY", "Azure:OpenAI:ApiKey"),
                OpenAIEndpoint = await GetSecretAsync("AZURE-OPENAI-ENDPOINT", "Azure:OpenAI:Endpoint"),
                OpenAIDeployment = await GetSecretAsync("AZURE-OPENAI-DEPLOYMENT", "Azure:OpenAI:Deployment") ?? "gpt-4",

                // Microsoft Graph
                GraphClientId = await GetSecretAsync("MICROSOFT-GRAPH-CLIENT-ID", "Azure:Graph:ClientId"),
                GraphTenantId = await GetSecretAsync("MICROSOFT-GRAPH-TENANT-ID", "Azure:Graph:TenantId"),
                GraphClientSecret = await GetSecretAsync("MICROSOFT-GRAPH-CLIENT-SECRET", "Azure:Graph:ClientSecret"),

                // Azure AI Foundry (AI Studio)
                AIFoundryEndpoint = await GetSecretAsync("AZURE-AI-FOUNDRY-ENDPOINT", "Azure:AIFoundry:Endpoint"),
                AIFoundryApiKey = await GetSecretAsync("AZURE-AI-FOUNDRY-API-KEY", "Azure:AIFoundry:ApiKey"),
                AIFoundryProjectId = await GetSecretAsync("AZURE-AI-FOUNDRY-PROJECT-ID", "Azure:AIFoundry:ProjectId"),

                // Email Configuration
                DefaultSenderEmail = await GetSecretAsync("DEFAULT-SENDER-EMAIL", "Email:DefaultSender"),
                DefaultRecipientEmail = await GetSecretAsync("DEFAULT-RECIPIENT-EMAIL", "Email:DefaultRecipient")
            };
        }
    }

    public class AzureCredentials
    {
        // Azure OpenAI
        public string? OpenAIApiKey { get; set; }
        public string? OpenAIEndpoint { get; set; }
        public string? OpenAIDeployment { get; set; }

        // Microsoft Graph
        public string? GraphClientId { get; set; }
        public string? GraphTenantId { get; set; }
        public string? GraphClientSecret { get; set; }

        // Azure AI Foundry
        public string? AIFoundryEndpoint { get; set; }
        public string? AIFoundryApiKey { get; set; }
        public string? AIFoundryProjectId { get; set; }

        // Email Configuration
        public string? DefaultSenderEmail { get; set; }
        public string? DefaultRecipientEmail { get; set; }

        public bool IsOpenAIConfigured => !string.IsNullOrEmpty(OpenAIApiKey) && !string.IsNullOrEmpty(OpenAIEndpoint);
        public bool IsGraphConfigured => !string.IsNullOrEmpty(GraphClientId) && !string.IsNullOrEmpty(GraphTenantId);
        public bool IsAIFoundryConfigured => !string.IsNullOrEmpty(AIFoundryEndpoint) && !string.IsNullOrEmpty(AIFoundryApiKey);
    }
}