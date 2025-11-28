using System.Text.Json.Serialization;

namespace LeadResearchAgent.Models
{
    public class Meta
    {
        [JsonPropertyName("fuente_seccion")]
        public string? FuenteSeccion { get; set; }

        [JsonPropertyName("linea_original")]
        public string? LineaOriginal { get; set; }
    }
}
