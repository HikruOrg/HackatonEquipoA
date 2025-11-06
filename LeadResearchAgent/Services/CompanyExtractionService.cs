using Microsoft.SemanticKernel;
using LeadResearchAgent.Models;
using Newtonsoft.Json;

namespace LeadResearchAgent.Services
{
    public class CompanyExtractionService
    {
        private readonly Kernel _kernel;

        public CompanyExtractionService(Kernel kernel)
        {
            _kernel = kernel;
        }

        public async Task<List<Company>> ExtractCompaniesFromNewsletterAsync(string newsletterText)
        {
            try
            {
                var extractionPrompt = @"
You are an AI assistant specialized in extracting company information from newsletter text.

Extract company information from the following newsletter text and return ONLY a valid JSON array of companies.
Each company object must have exactly these properties:
- company: Company name
- round: Funding round (e.g., ""Series A"", ""Seed"", ""Series B"", etc.)
- amount: Funding amount with currency (e.g., ""$5M"", ""€2M"", ""$10M"")
- sector: Industry/sector the company operates in
- HQ: Company headquarters location (city, country)
- snippet: Brief description or key information about the company (max 200 chars)

Newsletter text:
{{$newsletterText}}

Return only the JSON array, no additional text or explanation.
Example format:
[
  {
    ""company"": ""TechCorp"",
    ""round"": ""Series A"",
    ""amount"": ""$5M"",
    ""sector"": ""FinTech"",
    ""HQ"": ""San Francisco, USA"",
    ""snippet"": ""AI-powered financial analytics platform for small businesses""
  }
]";

                var function = _kernel.CreateFunctionFromPrompt(extractionPrompt);
                var result = await _kernel.InvokeAsync(function, new() { ["newsletterText"] = newsletterText });

                var jsonResponse = result.ToString().Trim();
                
                // Clean up the response to ensure it's valid JSON
                if (jsonResponse.StartsWith("```json"))
                {
                    jsonResponse = jsonResponse.Substring(7);
                }
                if (jsonResponse.EndsWith("```"))
                {
                    jsonResponse = jsonResponse.Substring(0, jsonResponse.Length - 3);
                }

                var companies = JsonConvert.DeserializeObject<List<Company>>(jsonResponse);
                return companies ?? new List<Company>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️  AI extraction failed: {ex.Message}");
                Console.WriteLine("🔄 Falling back to mock extraction for demo purposes...");
                
                // For hackathon demo - return pre-extracted data
                return MockAIService.ExtractMockCompanies();
            }
        }
    }
}