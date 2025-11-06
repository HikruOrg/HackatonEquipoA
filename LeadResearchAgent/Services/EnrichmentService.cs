using CsvHelper;
using CsvHelper.Configuration;
using LeadResearchAgent.Models;
using System.Globalization;

namespace LeadResearchAgent.Services
{
    public class EnrichmentService
    {
        private readonly List<EnrichmentData> _enrichmentData;

        public EnrichmentService(string csvFilePath)
        {
            _enrichmentData = LoadEnrichmentData(csvFilePath);
        }

        private List<EnrichmentData> LoadEnrichmentData(string csvFilePath)
        {
            var enrichmentData = new List<EnrichmentData>();

            try
            {
                if (!File.Exists(csvFilePath))
                {
                    Console.WriteLine($"Enrichment CSV file not found: {csvFilePath}");
                    return enrichmentData;
                }

                using var reader = new StringReader(File.ReadAllText(csvFilePath));
                using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
                
                csv.Context.RegisterClassMap<EnrichmentDataMap>();
                enrichmentData = csv.GetRecords<EnrichmentData>().ToList();
                
                Console.WriteLine($"Loaded {enrichmentData.Count} enrichment records from {csvFilePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading enrichment data: {ex.Message}");
            }

            return enrichmentData;
        }

        public void EnrichCompany(Company company)
        {
            if (string.IsNullOrEmpty(company.Name))
                return;

            // Try to find enrichment data by company name
            var enrichmentRecord = FindEnrichmentData(company.Name);
            
            if (enrichmentRecord != null)
            {
                company.Domain = enrichmentRecord.Domain;
                company.HeadCount = enrichmentRecord.HeadCount;
                
                Console.WriteLine($"Enriched {company.Name} with domain: {company.Domain}, headcount: {company.HeadCount}");
            }
            else
            {
                Console.WriteLine($"No enrichment data found for {company.Name}");
            }
        }

        private EnrichmentData? FindEnrichmentData(string companyName)
        {
            // Exact match first
            var exactMatch = _enrichmentData.FirstOrDefault(e => 
                string.Equals(e.CompanyName, companyName, StringComparison.OrdinalIgnoreCase));
            
            if (exactMatch != null)
                return exactMatch;

            // Fuzzy matching
            var normalizedCompanyName = NormalizeCompanyName(companyName);
            
            foreach (var record in _enrichmentData)
            {
                var normalizedRecordName = NormalizeCompanyName(record.CompanyName);
                
                // Check if names are similar (contains or partial match)
                if (normalizedRecordName.Contains(normalizedCompanyName) || 
                    normalizedCompanyName.Contains(normalizedRecordName))
                {
                    return record;
                }
                
                // Check similarity by removing common suffixes
                if (AreCompaniesSimilar(normalizedCompanyName, normalizedRecordName))
                {
                    return record;
                }
            }

            return null;
        }

        private string NormalizeCompanyName(string companyName)
        {
            if (string.IsNullOrEmpty(companyName))
                return string.Empty;

            return companyName.ToLowerInvariant()
                            .Replace("inc.", "")
                            .Replace("inc", "")
                            .Replace("ltd.", "")
                            .Replace("ltd", "")
                            .Replace("llc", "")
                            .Replace("corp.", "")
                            .Replace("corp", "")
                            .Replace("co.", "")
                            .Replace("&", "and")
                            .Replace("-", " ")
                            .Replace(".", "")
                            .Replace(",", "")
                            .Trim();
        }

        private bool AreCompaniesSimilar(string name1, string name2)
        {
            if (string.IsNullOrEmpty(name1) || string.IsNullOrEmpty(name2))
                return false;

            // Split into words and check if they share significant words
            var words1 = name1.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var words2 = name2.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (words1.Length == 0 || words2.Length == 0)
                return false;

            // Count matching words (excluding very short words)
            var matchingWords = words1.Intersect(words2)
                                    .Where(w => w.Length > 2)
                                    .Count();

            // If more than half of the words match, consider them similar
            var minWords = Math.Min(words1.Length, words2.Length);
            return matchingWords > 0 && (double)matchingWords / minWords >= 0.5;
        }
    }

    public class EnrichmentDataMap : ClassMap<EnrichmentData>
    {
        public EnrichmentDataMap()
        {
            Map(m => m.CompanyName).Name("company", "company_name", "name");
            Map(m => m.Domain).Name("domain", "website", "url");
            Map(m => m.HeadCount).Name("headcount", "head_count", "employees", "employee_count");
        }
    }
}