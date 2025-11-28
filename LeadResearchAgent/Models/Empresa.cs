using System.Text.Json.Serialization;

namespace LeadResearchAgent.Models
{
    public class Empresa
    {
        [JsonPropertyName("nombre")]
        public string? Nombre { get; set; }

        [JsonPropertyName("pais")]
        public string? Pais { get; set; }

        [JsonPropertyName("sector")]
        public string? Sector { get; set; }

        [JsonPropertyName("dominio")]
        public string? Dominio { get; set; }
    }
}
