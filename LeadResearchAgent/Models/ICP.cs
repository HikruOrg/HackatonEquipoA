using Newtonsoft.Json;

namespace LeadResearchAgent.Models
{
    public class ICP
    {
        [JsonProperty("industry")]
        public string[] Industry { get; set; } = Array.Empty<string>();

        [JsonProperty("stage")]
        public string[] Stage { get; set; } = Array.Empty<string>();

        [JsonProperty("size")]
        public SizeRange Size { get; set; } = new SizeRange();

        [JsonProperty("geo")]
        public string[] Geography { get; set; } = Array.Empty<string>();

        [JsonProperty("tech_hints")]
        public string[] TechHints { get; set; } = Array.Empty<string>();
    }

    public class SizeRange
    {
        [JsonProperty("min_employees")]
        public int MinEmployees { get; set; }

        [JsonProperty("max_employees")]
        public int MaxEmployees { get; set; }

        [JsonProperty("funding_min")]
        public string FundingMin { get; set; } = string.Empty;

        [JsonProperty("funding_max")]
        public string FundingMax { get; set; } = string.Empty;
    }
}