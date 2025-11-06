using LeadResearchAgent.Models;
using Newtonsoft.Json;

namespace LeadResearchAgent.Services
{
    public class ICPScoringService
    {
        private readonly ICP _icp;

        public ICPScoringService(ICP icp)
        {
            _icp = icp;
        }

        public static ICPScoringService LoadFromFile(string icpFilePath)
        {
            var json = File.ReadAllText(icpFilePath);
            var icp = JsonConvert.DeserializeObject<ICP>(json) ?? new ICP();
            return new ICPScoringService(icp);
        }

        public double ScoreCompany(Company company)
        {
            double totalScore = 0;
            int criteriaCount = 0;

            // Industry match (weight: 25%)
            if (_icp.Industry.Length > 0)
            {
                var industryScore = ScoreIndustry(company.Sector);
                totalScore += industryScore * 0.25;
                criteriaCount++;
            }

            // Stage match (weight: 20%)
            if (_icp.Stage.Length > 0)
            {
                var stageScore = ScoreStage(company.Round);
                totalScore += stageScore * 0.20;
                criteriaCount++;
            }

            // Geography match (weight: 15%)
            if (_icp.Geography.Length > 0)
            {
                var geoScore = ScoreGeography(company.Headquarters);
                totalScore += geoScore * 0.15;
                criteriaCount++;
            }

            // Size match (weight: 20%)
            var sizeScore = ScoreSize(company);
            totalScore += sizeScore * 0.20;
            criteriaCount++;

            // Tech hints match (weight: 20%)
            if (_icp.TechHints.Length > 0)
            {
                var techScore = ScoreTechHints(company.Snippet);
                totalScore += techScore * 0.20;
                criteriaCount++;
            }

            return criteriaCount > 0 ? totalScore : 0;
        }

        private double ScoreIndustry(string companySector)
        {
            if (string.IsNullOrEmpty(companySector)) return 0;

            foreach (var industry in _icp.Industry)
            {
                if (companySector.Contains(industry, StringComparison.OrdinalIgnoreCase) ||
                    industry.Contains(companySector, StringComparison.OrdinalIgnoreCase))
                {
                    return 1.0; // Perfect match
                }
            }

            // Partial matching with industry keywords
            var industryKeywords = _icp.Industry.SelectMany(i => i.Split(' ', '-', '&'))
                                                .Where(k => k.Length > 2)
                                                .ToArray();

            foreach (var keyword in industryKeywords)
            {
                if (companySector.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                {
                    return 0.6; // Partial match
                }
            }

            return 0;
        }

        private double ScoreStage(string companyRound)
        {
            if (string.IsNullOrEmpty(companyRound)) return 0;

            foreach (var stage in _icp.Stage)
            {
                if (companyRound.Contains(stage, StringComparison.OrdinalIgnoreCase))
                {
                    return 1.0; // Perfect match
                }
            }

            return 0;
        }

        private double ScoreGeography(string companyHQ)
        {
            if (string.IsNullOrEmpty(companyHQ)) return 0;

            foreach (var geo in _icp.Geography)
            {
                if (companyHQ.Contains(geo, StringComparison.OrdinalIgnoreCase))
                {
                    return 1.0; // Perfect match
                }
            }

            return 0;
        }

        private double ScoreSize(Company company)
        {
            double score = 0;
            int sizeFactors = 0;

            // Score based on employee count if available
            if (company.HeadCount.HasValue && _icp.Size.MaxEmployees > 0)
            {
                if (company.HeadCount >= _icp.Size.MinEmployees && 
                    company.HeadCount <= _icp.Size.MaxEmployees)
                {
                    score += 1.0;
                }
                else if (company.HeadCount >= _icp.Size.MinEmployees * 0.5 && 
                         company.HeadCount <= _icp.Size.MaxEmployees * 1.5)
                {
                    score += 0.6; // Close to range
                }
                sizeFactors++;
            }

            // Score based on funding amount
            if (!string.IsNullOrEmpty(company.Amount) && 
                !string.IsNullOrEmpty(_icp.Size.FundingMin))
            {
                var fundingScore = ScoreFundingAmount(company.Amount);
                score += fundingScore;
                sizeFactors++;
            }

            return sizeFactors > 0 ? score / sizeFactors : 0.5; // Default moderate score if no size info
        }

        private double ScoreFundingAmount(string amount)
        {
            // Simple funding amount scoring
            // Extract numeric value from funding amount
            var numericValue = ExtractNumericValue(amount);
            var minFunding = ExtractNumericValue(_icp.Size.FundingMin);
            var maxFunding = !string.IsNullOrEmpty(_icp.Size.FundingMax) ? 
                           ExtractNumericValue(_icp.Size.FundingMax) : double.MaxValue;

            if (numericValue >= minFunding && numericValue <= maxFunding)
            {
                return 1.0;
            }
            else if (numericValue >= minFunding * 0.5 && numericValue <= maxFunding * 1.5)
            {
                return 0.6;
            }

            return 0;
        }

        private double ExtractNumericValue(string amount)
        {
            if (string.IsNullOrEmpty(amount)) return 0;

            // Remove currency symbols and convert to lowercase
            var cleanAmount = amount.ToLower()
                                   .Replace("$", "")
                                   .Replace("€", "")
                                   .Replace("£", "")
                                   .Replace(",", "")
                                   .Trim();

            // Handle M (millions) and K (thousands)
            double multiplier = 1;
            if (cleanAmount.EndsWith("m"))
            {
                multiplier = 1_000_000;
                cleanAmount = cleanAmount.Substring(0, cleanAmount.Length - 1);
            }
            else if (cleanAmount.EndsWith("k"))
            {
                multiplier = 1_000;
                cleanAmount = cleanAmount.Substring(0, cleanAmount.Length - 1);
            }

            if (double.TryParse(cleanAmount, out double value))
            {
                return value * multiplier;
            }

            return 0;
        }

        private double ScoreTechHints(string snippet)
        {
            if (string.IsNullOrEmpty(snippet)) return 0;

            int matches = 0;
            foreach (var hint in _icp.TechHints)
            {
                if (snippet.Contains(hint, StringComparison.OrdinalIgnoreCase))
                {
                    matches++;
                }
            }

            return _icp.TechHints.Length > 0 ? (double)matches / _icp.TechHints.Length : 0;
        }
    }
}