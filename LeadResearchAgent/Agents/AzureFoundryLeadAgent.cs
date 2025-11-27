using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.AI.Agents.Persistent;
using Azure.Identity;

namespace LeadResearchAgent.Agents
{
    /// <summary>
    /// Client that delegates newsletter analysis to an Azure Foundry agent.
    /// The remote agent is expected to return a JSON array following the schema
    /// provided by the user (see FoundryCompanyResult).
    /// </summary>
    public class AzureFoundryLeadAgent
    {
        private readonly PersistentAgentsClient _persistentClient;
        private readonly string _endpoint;
        private readonly string _assistantId;
        private readonly HttpClient _http;

        /// <summary>
        /// Create the client. If <paramref name="httpClient"/> is null, a default HttpClient is created.
        /// The endpoint, apiKey and assistantId can be provided here or via environment variables:
        /// - AZURE_FOUNDRY_ENDPOINT
        /// - AZURE_FOUNDRY_API_KEY
        /// - AZURE_FOUNDRY_AGENT_ID
        /// </summary>
        public AzureFoundryLeadAgent(string? endpoint = null, string? apiKey = null, string? assistantId = null, HttpClient? httpClient = null, TimeSpan? timeout = null)
        {
            _endpoint = endpoint ?? Environment.GetEnvironmentVariable("AZURE_FOUNDRY_ENDPOINT") ?? throw new InvalidOperationException("Endpoint required");
            _assistantId = assistantId ?? Environment.GetEnvironmentVariable("AZURE_FOUNDRY_AGENT_ID") ?? throw new InvalidOperationException("Agent id required");
            var key = apiKey ?? Environment.GetEnvironmentVariable("AZURE_FOUNDRY_API_KEY") ?? throw new InvalidOperationException("API key required");

            _http = httpClient ?? new HttpClient();
            if (timeout.HasValue) _http.Timeout = timeout.Value;

            // Use AzureKeyCredential for API key auth and Uri for endpoint
            _persistentClient = new PersistentAgentsClient(_endpoint, new AzureCliCredential());
        }

        /// <summary>
        /// Sends the newsletterText (and optional email id/metadata) to the Azure Foundry agent and
        /// returns the parsed results as a list of FoundryCompanyResult.
        /// The assistant/agent id is included in the request body so the Foundry endpoint can route to the correct agent.
        /// </summary>
        public async Task<List<FoundryCompanyResult>> ProcessNewsletterAsync(string newsletterText, string? idEmail = null, CancellationToken cancellationToken = default)
        {
            // Use parameterless constructor and object initializer
            var threadOptions = new PersistentAgentThreadCreationOptions
            {
                // Messages property is IList<ThreadMessageOptions> — use initializer directly
            };
            threadOptions.Messages.Add(new ThreadMessageOptions
            ("user", newsletterText));

            var options = new ThreadAndRunOptions
            {
                ThreadOptions = threadOptions
            };



            var runResponse = await _persistentClient.CreateThreadAndRunAsync(_assistantId, options, cancellationToken).ConfigureAwait(false);
            var run = runResponse.Value;

            // Wait for the run to complete
            while (run.Status == "queued" || run.Status == "in_progress")
            {
                await Task.Delay(1000, cancellationToken);
                var statusResponse = await _persistentClient.Runs.GetRunAsync(run.ThreadId, run.Id, cancellationToken).ConfigureAwait(false);
                run = statusResponse.Value;
            }

            if (run.Status != "completed")
            {
                throw new InvalidOperationException($"Agent run failed with status: {run.Status}");
            }

            // Get messages from the thread
            //var messagesResponse = await _persistentClient.GetMessagesAsync(run.ThreadId, cancellationToken: cancellationToken).ConfigureAwait(false);

            var messagesPageable = _persistentClient.Messages.GetMessagesAsync(run.ThreadId);

            var messages = new List<PersistentThreadMessage>();
            await foreach (var message in messagesPageable)
            {
                messages.Add(message);
            }

            // Extract the assistant's response (most recent message with role "assistant")
            var assistantMessage = messages
                .Where(m => m.Role == "assistant")
                .OrderByDescending(m => m.CreatedAt)
                .FirstOrDefault();

            if (assistantMessage == null)
            {
                throw new InvalidOperationException("No assistant response found in thread");
            }

            // Extract text content from message
            var outputText = ExtractTextFromMessage(assistantMessage);

            var parsedOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var parsed = JsonSerializer.Deserialize<List<FoundryCompanyResult>>(outputText, parsedOptions)
                         ?? new List<FoundryCompanyResult>();

            return parsed;
        }

        private static string ExtractTextFromMessage(PersistentThreadMessage message)
        {
            // Messages contain a list of content items; find text content
            foreach (var contentItem in message.ContentItems)
            {

                if (contentItem is MessageTextContent textContent)
                {
                    return textContent.Text ?? string.Empty;
                }

            }

            return string.Empty;
        }
    }

    // Models to match the JSON structure expected from the Foundry agent.
    // JsonPropertyName attributes map exact JSON keys (including spaces) to C# properties.

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

    public class CamposRelevantes
    {
        [JsonPropertyName("contacto")]
        public string? Contacto { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("telefono")]
        public string? Telefono { get; set; }
    }

    public class Meta
    {
        [JsonPropertyName("fuente_seccion")]
        public string? FuenteSeccion { get; set; }

        [JsonPropertyName("linea_original")]
        public string? LineaOriginal { get; set; }
    }
}
