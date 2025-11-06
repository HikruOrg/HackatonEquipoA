using Microsoft.SemanticKernel;
using LeadResearchAgent.Models;
using LeadResearchAgent.Services;

namespace LeadResearchAgent.Agents
{
    public class LeadResearchAgent
    {
        private readonly Kernel _kernel;
        private readonly CompanyExtractionService _extractionService;
        private readonly ICPScoringService _scoringService;
        private readonly OutreachMessageService _outreachService;
        private readonly EnrichmentService _enrichmentService;
        private readonly ICP _icp;

        public LeadResearchAgent(
            Kernel kernel,
            string icpFilePath,
            string enrichmentCsvPath)
        {
            _kernel = kernel;
            _extractionService = new CompanyExtractionService(kernel);
            _scoringService = ICPScoringService.LoadFromFile(icpFilePath);
            _outreachService = new OutreachMessageService(kernel);
            _enrichmentService = new EnrichmentService(enrichmentCsvPath);
            
            // Load ICP for outreach message generation
            var icpJson = File.ReadAllText(icpFilePath);
            _icp = Newtonsoft.Json.JsonConvert.DeserializeObject<ICP>(icpJson) ?? new ICP();
        }

        public async Task<List<Company>> ProcessNewsletterAsync(string newsletterText, double minimumICPScore = 0.3)
        {
            Console.WriteLine("🔍 Starting Lead Research Agent processing...");
            
            // Step 1: Extract companies from newsletter
            Console.WriteLine("\n📄 Step 1: Extracting companies from newsletter...");
            var extractedCompanies = await _extractionService.ExtractCompaniesFromNewsletterAsync(newsletterText);
            Console.WriteLine($"Extracted {extractedCompanies.Count} companies");

            if (!extractedCompanies.Any())
            {
                Console.WriteLine("❌ No companies found in newsletter");
                return new List<Company>();
            }

            var processedCompanies = new List<Company>();

            foreach (var company in extractedCompanies)
            {
                Console.WriteLine($"\n🏢 Processing: {company.Name}");
                
                // Step 2: Enrich with CSV data
                _enrichmentService.EnrichCompany(company);
                
                // Step 3: Score against ICP
                company.ICPScore = _scoringService.ScoreCompany(company);
                Console.WriteLine($"ICP Score: {company.ICPScore:F2}");
                
                // Filter by minimum ICP score
                if (company.ICPScore >= minimumICPScore)
                {
                    // Step 4: Generate outreach message
                    Console.WriteLine("✅ Company matches ICP criteria - generating outreach...");
                    company.OutreachMessage = await _outreachService.GenerateOutreachMessageAsync(company, _icp);
                    
                    processedCompanies.Add(company);
                    Console.WriteLine($"Outreach: {company.OutreachMessage}");
                }
                else
                {
                    Console.WriteLine($"❌ Company doesn't meet minimum ICP score ({minimumICPScore:F2})");
                }
            }

            // Sort by ICP score (highest first)
            processedCompanies = processedCompanies.OrderByDescending(c => c.ICPScore).ToList();

            Console.WriteLine($"\n🎯 Final Results: {processedCompanies.Count} companies match your ICP");
            return processedCompanies;
        }

        public void PrintResults(List<Company> companies)
        {
            Console.WriteLine("\n" + new string('=', 80));
            Console.WriteLine("🎯 LEAD RESEARCH AGENT RESULTS");
            Console.WriteLine(new string('=', 80));

            if (!companies.Any())
            {
                Console.WriteLine("No companies found that match your ICP criteria.");
                return;
            }

            for (int i = 0; i < companies.Count; i++)
            {
                var company = companies[i];
                Console.WriteLine($"\n#{i + 1} - {company.Name} (Score: {company.ICPScore:F2})");
                Console.WriteLine(new string('-', 50));
                Console.WriteLine($"Sector: {company.Sector}");
                Console.WriteLine($"Round: {company.Round} | Amount: {company.Amount}");
                Console.WriteLine($"HQ: {company.Headquarters}");
                if (!string.IsNullOrEmpty(company.Domain))
                    Console.WriteLine($"Domain: {company.Domain}");
                if (company.HeadCount.HasValue)
                    Console.WriteLine($"Employees: {company.HeadCount}");
                Console.WriteLine($"Snippet: {company.Snippet}");
                Console.WriteLine($"\n💬 Outreach Message:");
                Console.WriteLine($"   {company.OutreachMessage}");
            }

            Console.WriteLine("\n" + new string('=', 80));
        }

        public async Task<string> ExportToJsonAsync(List<Company> companies, string outputPath)
        {
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(companies, Newtonsoft.Json.Formatting.Indented);
            await File.WriteAllTextAsync(outputPath, json);
            Console.WriteLine($"✅ Results exported to: {outputPath}");
            return json;
        }

        public ICP LoadICP()
        {
            return _icp;
        }
    }
}