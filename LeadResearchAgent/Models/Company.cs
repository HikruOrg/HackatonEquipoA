using Newtonsoft.Json;

namespace LeadResearchAgent.Models
{
    public class Company
    {
        [JsonProperty("company")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("round")]
        public string Round { get; set; } = string.Empty;

        [JsonProperty("amount")]
        public string Amount { get; set; } = string.Empty;

        [JsonProperty("sector")]
        public string Sector { get; set; } = string.Empty;

        [JsonProperty("HQ")]
        public string Headquarters { get; set; } = string.Empty;

        [JsonProperty("snippet")]
        public string Snippet { get; set; } = string.Empty;

        // Additional enrichment data
        public string? Domain { get; set; }
        public int? HeadCount { get; set; }
        public double ICPScore { get; set; } = 0.0;
        public string OutreachMessage { get; set; } = string.Empty;
    }
}