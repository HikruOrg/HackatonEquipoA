using System.Text.Json.Serialization;

namespace LeadResearchAgent.Models
{
    public class CamposRelevantes
    {
        [JsonPropertyName("contacto")]
        public string? Contacto { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("telefono")]
        public string? Telefono { get; set; }
    }
}
