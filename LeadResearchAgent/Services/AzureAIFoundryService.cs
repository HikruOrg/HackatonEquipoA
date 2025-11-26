using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using LeadResearchAgent.Models;

namespace LeadResearchAgent.Services
{
    public class AzureAIFoundryService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<AzureAIFoundryService>? _logger;
        private readonly string? _endpoint;
        private readonly string? _apiKey;
        private readonly string? _projectId;

        public AzureAIFoundryService(HttpClient httpClient, string? endpoint, string? apiKey, string? projectId, ILogger<AzureAIFoundryService>? logger = null)
        {
            _httpClient = httpClient;
            _endpoint = endpoint;
            _apiKey = apiKey;
            _projectId = projectId;
            _logger = logger;

            // Configure HTTP client
            if (!string.IsNullOrEmpty(_apiKey))
            {
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
            }
        }

        public bool IsConfigured => !string.IsNullOrEmpty(_endpoint) && !string.IsNullOrEmpty(_apiKey);

        /// <summary>
        /// Enhances ICP scoring using Azure AI Foundry models
        /// </summary>
        public async Task<double> EnhanceICPScoringAsync(Company company, ICP icp)
        {
            if (!IsConfigured)
            {
                _logger?.LogWarning("Azure AI Foundry not configured, returning default score");
                return company.ICPScore;
            }

            try
            {
                var prompt = GenerateICPScoringPrompt(company, icp);
                var response = await CallAIFoundryModelAsync(prompt, "icp-scoring");
                
                // Parse the response to extract enhanced score
                if (double.TryParse(response.Trim(), out var enhancedScore))
                {
                    return Math.Max(0, Math.Min(1, enhancedScore));
                }

                _logger?.LogWarning($"Failed to parse AI Foundry ICP score response: {response}");
                return company.ICPScore;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to enhance ICP scoring with AI Foundry");
                return company.ICPScore;
            }
        }

        /// <summary>
        /// Generates enhanced outreach messages using Azure AI Foundry
        /// </summary>
        public async Task<string> GenerateEnhancedOutreachAsync(Company company, ICP icp, string context = "")
        {
            if (!IsConfigured)
            {
                _logger?.LogWarning("Azure AI Foundry not configured, using basic outreach");
                return GenerateBasicOutreach(company);
            }

            try
            {
                var prompt = GenerateOutreachPrompt(company, icp, context);
                var response = await CallAIFoundryModelAsync(prompt, "outreach-generation");
                
                return !string.IsNullOrEmpty(response) ? response.Trim() : GenerateBasicOutreach(company);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to generate enhanced outreach with AI Foundry");
                return GenerateBasicOutreach(company);
            }
        }

        /// <summary>
        /// Extracts and enriches company data using AI Foundry
        /// </summary>
        public async Task<Company> EnrichCompanyDataAsync(Company company, string additionalContext = "")
        {
            if (!IsConfigured)
            {
                return company;
            }

            try
            {
                var prompt = GenerateEnrichmentPrompt(company, additionalContext);
                var response = await CallAIFoundryModelAsync(prompt, "company-enrichment");
                
                // Parse JSON response to update company data
                if (!string.IsNullOrEmpty(response))
                {
                    var enrichedData = JsonSerializer.Deserialize<CompanyEnrichment>(response);
                    if (enrichedData != null)
                    {
                        ApplyEnrichment(company, enrichedData);
                    }
                }

                return company;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to enrich company data with AI Foundry");
                return company;
            }
        }

        /// <summary>
        /// Analyzes market trends and provides insights
        /// </summary>
        public async Task<MarketInsights> AnalyzeMarketTrendsAsync(List<Company> companies)
        {
            if (!IsConfigured)
            {
                return new MarketInsights
                {
                    Summary = "AI Foundry not configured - basic analysis only",
                    TopSectors = companies.GroupBy(c => c.Sector).OrderByDescending(g => g.Count()).Take(3).Select(g => g.Key).ToList(),
                    TrendingKeywords = new List<string>(),
                    Recommendations = new List<string> { "Configure Azure AI Foundry for enhanced insights" }
                };
            }

            try
            {
                var prompt = GenerateMarketAnalysisPrompt(companies);
                var response = await CallAIFoundryModelAsync(prompt, "market-analysis");
                
                var insights = JsonSerializer.Deserialize<MarketInsights>(response);
                return insights ?? new MarketInsights { Summary = "Failed to analyze market trends" };
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to analyze market trends with AI Foundry");
                return new MarketInsights { Summary = $"Error analyzing trends: {ex.Message}" };
            }
        }

        private async Task<string> CallAIFoundryModelAsync(string prompt, string taskType)
        {
            if (string.IsNullOrEmpty(_endpoint))
            {
                throw new InvalidOperationException("Azure AI Foundry endpoint not configured");
            }

            var requestBody = new
            {
                messages = new[]
                {
                    new { role = "system", content = GetSystemPrompt(taskType) },
                    new { role = "user", content = prompt }
                },
                max_tokens = 1000,
                temperature = 0.3,
                top_p = 0.9
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var requestUrl = $"{_endpoint.TrimEnd('/')}/openai/deployments/gpt-4/chat/completions?api-version=2024-02-15-preview";
            
            _logger?.LogInformation($"Calling AI Foundry for {taskType}");
            
            var response = await _httpClient.PostAsync(requestUrl, content);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var responseObj = JsonSerializer.Deserialize<JsonElement>(responseContent);
            
            if (responseObj.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
            {
                var firstChoice = choices[0];
                if (firstChoice.TryGetProperty("message", out var message) &&
                    message.TryGetProperty("content", out var messageContent))
                {
                    return messageContent.GetString() ?? "";
                }
            }

            return "";
        }

        private string GetSystemPrompt(string taskType)
        {
            return taskType switch
            {
                "icp-scoring" => "You are an expert lead scoring analyst. Analyze the company against the ICP criteria and return a single decimal score between 0 and 1.",
                "outreach-generation" => "You are an expert sales development representative. Generate personalized, professional outreach messages that are compelling but not pushy.",
                "company-enrichment" => "You are a company research specialist. Provide additional insights and data about companies in JSON format.",
                "market-analysis" => "You are a market research analyst. Analyze company data to identify trends, patterns, and strategic insights.",
                _ => "You are a helpful AI assistant specializing in business intelligence and lead research."
            };
        }

        private string GenerateICPScoringPrompt(Company company, ICP icp)
        {
            return $@"
Analyze this company against our Ideal Customer Profile (ICP) and provide a score from 0 to 1:

Company: {company.Name}
Sector: {company.Sector}
Funding: {company.Round} - {company.Amount}
Location: {company.Headquarters}
Description: {company.Snippet}

ICP Criteria:
- Target Industries: {string.Join(", ", icp.Industry)}
- Target Stages: {string.Join(", ", icp.Stage)}
- Target Geography: {string.Join(", ", icp.Geography)}
- Tech Keywords: {string.Join(", ", icp.TechHints)}
- Employee Range: {icp.Size.MinEmployees}-{icp.Size.MaxEmployees}
- Funding Range: {icp.Size.FundingMin}-{icp.Size.FundingMax}

Current Score: {company.ICPScore:F3}

Consider: industry alignment, stage fit, geographic preference, tech relevance, funding range, and growth potential.
Return only the numerical score (e.g., 0.85).";
        }

        private string GenerateOutreachPrompt(Company company, ICP icp, string context)
        {
            return $@"
Generate a personalized outreach message for this company:

Company: {company.Name}
Sector: {company.Sector}
Funding: {company.Round} - {company.Amount}
Location: {company.Headquarters}
Description: {company.Snippet}
ICP Score: {company.ICPScore:F2}

Context: {context}

Requirements:
- 2-3 sentences maximum
- Professional but warm tone
- Acknowledge their recent success/funding
- Connect to how Hikru can help their growth
- Include a soft call-to-action
- No generic phrases

Generate the outreach message only.";
        }

        private string GenerateEnrichmentPrompt(Company company, string additionalContext)
        {
            return $@"
Enrich the following company data with additional insights:

Company: {company.Name}
Current Data: Sector: {company.Sector}, Funding: {company.Round} - {company.Amount}, Location: {company.Headquarters}
Description: {company.Snippet}
Additional Context: {additionalContext}

Provide enriched data in JSON format with these fields:
{{
  ""estimatedEmployees"": number,
  ""foundedYear"": number,
  ""website"": ""url"",
  ""technologies"": [""tech1"", ""tech2""],
  ""keyPeople"": [""person1"", ""person2""],
  ""recentNews"": ""brief summary"",
  ""competitorInsights"": ""brief analysis""
}}

Return only valid JSON.";
        }

        private string GenerateMarketAnalysisPrompt(List<Company> companies)
        {
            var sectors = companies.GroupBy(c => c.Sector).Select(g => $"{g.Key}: {g.Count()}").ToList();
            var avgScore = companies.Average(c => c.ICPScore);

            return $@"
Analyze these {companies.Count} companies and provide market insights:

Sector Distribution: {string.Join(", ", sectors)}
Average ICP Score: {avgScore:F2}
Top Companies: {string.Join(", ", companies.OrderByDescending(c => c.ICPScore).Take(5).Select(c => c.Name))}

Provide insights in JSON format:
{{
  ""summary"": ""brief market overview"",
  ""topSectors"": [""sector1"", ""sector2"", ""sector3""],
  ""trendingKeywords"": [""keyword1"", ""keyword2""],
  ""recommendations"": [""recommendation1"", ""recommendation2""],
  ""opportunityScore"": 0.0-1.0
}}

Return only valid JSON.";
        }

        private void ApplyEnrichment(Company company, CompanyEnrichment enrichment)
        {
            // Apply enrichment data to company object
            // This would extend the Company model to include additional fields
        }

        private string GenerateBasicOutreach(Company company)
        {
            return $"Hi! Noticed {company.Name}'s recent {company.Round} funding. " +
                   $"Would love to explore how Hikru could support your growth in {company.Sector}.";
        }
    }

    public class CompanyEnrichment
    {
        public int? EstimatedEmployees { get; set; }
        public int? FoundedYear { get; set; }
        public string? Website { get; set; }
        public List<string> Technologies { get; set; } = new();
        public List<string> KeyPeople { get; set; } = new();
        public string? RecentNews { get; set; }
        public string? CompetitorInsights { get; set; }
    }

    public class MarketInsights
    {
        public string Summary { get; set; } = "";
        public List<string> TopSectors { get; set; } = new();
        public List<string> TrendingKeywords { get; set; } = new();
        public List<string> Recommendations { get; set; } = new();
        public double OpportunityScore { get; set; }
    }
}