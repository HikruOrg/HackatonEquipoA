using Microsoft.SemanticKernel;
using LeadResearchAgent.Models;

namespace LeadResearchAgent.Services
{
    public class OutreachMessageService
    {
        private readonly Kernel _kernel;

        public OutreachMessageService(Kernel kernel)
        {
            _kernel = kernel;
        }

        public async Task<string> GenerateOutreachMessageAsync(Company company, ICP icp)
        {
            try
            {
                var outreachPrompt = @"
You are an expert sales development representative (SDR) who creates personalized outreach messages.

Generate a 2-line personalized outreach message for the following company based on their profile and our ICP match.

Company Information:
- Name: {{$companyName}}
- Sector: {{$sector}}
- Funding Round: {{$round}}
- Funding Amount: {{$amount}}
- Headquarters: {{$headquarters}}
- Description: {{$snippet}}
- ICP Score: {{$icpScore}}

Our Target Profile (ICP):
- Target Industries: {{$targetIndustries}}
- Target Stages: {{$targetStages}}
- Target Geography: {{$targetGeo}}
- Tech Focus: {{$techHints}}

Instructions:
1. First line: Acknowledge their recent funding/growth and mention something specific about their company
2. Second line: Connect how our solution (Hikru) could help them scale or solve a relevant challenge
3. Be conversational, not salesy
4. Keep it under 50 words total
5. Don't use generic phrases like ""I hope this email finds you well""

Example format:
""Congrats on your Series A! {{specific company insight}}.
{{value proposition connection}} - would love to explore how Hikru could support your growth.""

Generate only the 2-line message, no additional text.";

                var function = _kernel.CreateFunctionFromPrompt(outreachPrompt);
                
                var arguments = new KernelArguments
                {
                    ["companyName"] = company.Name,
                    ["sector"] = company.Sector,
                    ["round"] = company.Round,
                    ["amount"] = company.Amount,
                    ["headquarters"] = company.Headquarters,
                    ["snippet"] = company.Snippet,
                    ["icpScore"] = company.ICPScore.ToString("F2"),
                    ["targetIndustries"] = string.Join(", ", GetTargetIndustries(icp)),
                    ["targetStages"] = string.Join(", ", GetTargetStages(icp)),
                    ["targetGeo"] = string.Join(", ", GetTargetGeography(icp)),
                    ["techHints"] = string.Join(", ", GetTechHints(icp))
                };

                var result = await _kernel.InvokeAsync(function, arguments);
                return result.ToString().Trim();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️  AI outreach generation failed: {ex.Message}");
                Console.WriteLine("🔄 Using mock outreach message for demo...");
                
                // Fallback to mock service
                return MockAIService.GenerateMockOutreachMessage(company);
            }
        }

        private string[] GetTargetIndustries(ICP icp)
        {
            return icp?.Industry ?? Array.Empty<string>();
        }

        private string[] GetTargetStages(ICP icp)
        {
            return icp?.Stage ?? Array.Empty<string>();
        }

        private string[] GetTargetGeography(ICP icp)
        {
            return icp?.Geography ?? Array.Empty<string>();
        }

        private string[] GetTechHints(ICP icp)
        {
            return icp?.TechHints ?? Array.Empty<string>();
        }
    }
}