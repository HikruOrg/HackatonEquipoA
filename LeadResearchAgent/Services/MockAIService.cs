using LeadResearchAgent.Models;
using Newtonsoft.Json;

namespace LeadResearchAgent.Services
{
    /// <summary>
    /// Mock service for demonstration purposes when Azure OpenAI is not available
    /// </summary>
    public class MockAIService
    {
        public static List<Company> ExtractMockCompanies()
        {
            // This simulates what the AI would extract from the sample newsletter
            return new List<Company>
            {
                new Company
                {
                    Name = "TechFlow Analytics",
                    Round = "Series A",
                    Amount = "$8M",
                    Sector = "FinTech",
                    Headquarters = "San Francisco, USA",
                    Snippet = "AI-powered financial analytics platform that helps banks automate risk assessment and fraud detection"
                },
                new Company
                {
                    Name = "DataVision Corp",
                    Round = "Series B",
                    Amount = "$15M",
                    Sector = "Business Intelligence",
                    Headquarters = "London, UK",
                    Snippet = "Real-time business intelligence platform using machine learning for predictive analytics in financial services"
                },
                new Company
                {
                    Name = "CloudSync Solutions",
                    Round = "Seed",
                    Amount = "$4.5M",
                    Sector = "FinTech",
                    Headquarters = "Berlin, Germany",
                    Snippet = "Cloud-based payment orchestration platform enabling seamless integration with multiple payment providers through single API"
                },
                new Company
                {
                    Name = "AutoInsights",
                    Round = "Series A",
                    Amount = "$6M",
                    Sector = "AI",
                    Headquarters = "Munich, Germany",
                    Snippet = "AI-powered analytics platform for automotive manufacturers helping optimize supply chain operations and predict maintenance"
                },
                new Company
                {
                    Name = "SecureBank",
                    Round = "Series B",
                    Amount = "$25M",
                    Sector = "Cybersecurity",
                    Headquarters = "Amsterdam, Netherlands",
                    Snippet = "Cybersecurity platform specializing in protecting financial institutions from advanced persistent threats using machine learning"
                },
                new Company
                {
                    Name = "InvestorPro",
                    Round = "Seed",
                    Amount = "$7M",
                    Sector = "SaaS",
                    Headquarters = "Stockholm, Sweden",
                    Snippet = "Portfolio management SaaS tools for independent financial advisors featuring automated compliance reporting and client communication"
                },
                new Company
                {
                    Name = "TradingBot",
                    Round = "Pre-Series A",
                    Amount = "$3.2M",
                    Sector = "FinTech",
                    Headquarters = "Copenhagen, Denmark",
                    Snippet = "Algorithmic trading platform for retail investors using AI to democratize sophisticated trading strategies"
                }
            };
        }

        public static string GenerateMockOutreachMessage(Company company)
        {
            var templates = new[]
            {
                $"Congrats on your {company.Round}! Love how {company.Name} is transforming {company.Sector.ToLower()}.\nHikru could help scale your sales process - would love to explore how we could support your growth.",
                
                $"Exciting news about your {company.Amount} raise! {company.Name}'s approach to {ExtractKeyword(company.Snippet)} is impressive.\nOur platform could accelerate your customer acquisition - interested in a quick chat?",
                
                $"Just saw the announcement about {company.Name}'s funding round. Your work in {company.Sector.ToLower()} aligns perfectly with what we see in the market.\nHikru could be a great fit for your expansion plans - worth a conversation?",
                
                $"Congratulations on securing {company.Amount}! {company.Name} is clearly solving a real problem in {company.Sector.ToLower()}.\nWe help companies like yours scale efficiently - would love to share how Hikru could support your journey.",
                
                $"Amazing progress with your {company.Round} at {company.Name}! Your focus on {ExtractKeyword(company.Snippet)} resonates with our mission.\nHikru could complement your growth strategy beautifully - open to a brief discussion?"
            };

            var random = new Random(company.Name.GetHashCode()); // Deterministic randomness
            return templates[random.Next(templates.Length)];
        }

        private static string ExtractKeyword(string snippet)
        {
            var keywords = new[] { "automation", "AI", "analytics", "platform", "integration", "optimization", "prediction", "intelligence", "solutions" };
            
            foreach (var keyword in keywords)
            {
                if (snippet.ToLower().Contains(keyword))
                {
                    return keyword;
                }
            }
            
            return "innovation";
        }
    }
}