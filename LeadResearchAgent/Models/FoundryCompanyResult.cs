using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;
using LeadResearchAgent.Agents;

namespace LeadResearchAgent.Models
{
    public class FoundryCompanyResult
    {
        [JsonPropertyName("empresa")]
        public Empresa? Empresa { get; set; }

        [JsonPropertyName("razon_de_match")]
        public string? RazonDeMatch { get; set; }

        [JsonPropertyName("Total Capital")]
        public string? TotalCapital { get; set; }

        [JsonPropertyName("nivel_interes")]
        public string? NivelInteres { get; set; }

        [JsonPropertyName("campos_relevantes")]
        public CamposRelevantes? CamposRelevantes { get; set; }

        [JsonPropertyName("resumen")]
        public string? Resumen { get; set; }

        [JsonPropertyName("meta")]
        public Meta? Meta { get; set; }

        [JsonPropertyName("id_email")]
        public string? IdEmail { get; set; }
    }
}
