using System.Net.Http.Json;
using System.Text.Json;
using IBM.Data.Db2;

namespace Polar.Services
{
    public class MisionGenerationService
    {
        private readonly Db2ConnectionFactory _factory;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<MisionGenerationService> _logger;

        public MisionGenerationService(
            Db2ConnectionFactory factory,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<MisionGenerationService> logger)
        {
            _factory = factory;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        private sealed class GeminiMissionDto
        {
            public string Titulo { get; set; } = "";
            public string Descripcion { get; set; } = "";
            public string Tipo { get; set; } = "";
            public int Puntos { get; set; }
        }

        private sealed class GeminiRequestDto
        {
            public object[] contents { get; set; } = Array.Empty<object>();
        }

        public async Task<bool> HasAnyMissionAsync(CancellationToken cancellationToken = default)
        {
            using var conn = _factory.Create();
            await conn.OpenAsync(cancellationToken);

            var sql = "SELECT COUNT(*) FROM DB2INST1.MISION";
            using var cmd = new DB2Command(sql, conn);

            var result = await cmd.ExecuteScalarAsync(cancellationToken);
            var total = Convert.ToInt32(result);

            return total > 0;
        }

        public async Task GenerateDailyMissionsAsync(CancellationToken cancellationToken = default)
        {
            var apiKey =
                _configuration["GEMINI_API_KEY"] ??
                Environment.GetEnvironmentVariable("GEMINI_API_KEY");

            if (string.IsNullOrWhiteSpace(apiKey))
                throw new InvalidOperationException("No se encontró la variable de entorno GEMINI_API_KEY.");

            var prompt = """
            Eres un generador de misiones ecológicas para una app llamada Polar.

            Reglas obligatorias:
            - Devuelve SOLO JSON válido.
            - Devuelve exactamente 3 misiones.
            - No uses markdown, no uses explicaciones, no uses bloques de código.
            - Cada misión debe tener estas claves exactas:
              - titulo
              - descripcion
              - tipo
              - puntos
            - "puntos" debe ser un entero positivo entre 10 y 30.
            - Las misiones deben ser realistas, breves, accionables y relacionadas con sostenibilidad.
            - Varía los tipos entre hogar, consumo, movilidad, energía o comunidad.

            Formato exacto esperado:
            [
              {
                "titulo": "string",
                "descripcion": "string",
                "tipo": "string",
                "puntos": 10
              }
            ]
            """;

            var requestBody = new GeminiRequestDto
            {
                contents = new[]
                {
                    new
                    {
                        role = "user",
                        parts = new[]
                        {
                            new { text = prompt }
                        }
                    }
                }
            };

            var http = _httpClientFactory.CreateClient();
            http.Timeout = TimeSpan.FromSeconds(90);

            var url =
                $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={Uri.EscapeDataString(apiKey)}";

            using var response = await http.PostAsJsonAsync(url, requestBody, cancellationToken);
            var rawResponse = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Error llamando Gemini. Status: {Status}. Body: {Body}", response.StatusCode, rawResponse);
                throw new InvalidOperationException("Gemini no devolvió una respuesta válida.");
            }

            var generatedText = ExtractGeminiText(rawResponse);
            var missions = ParseMissions(generatedText);

            if (missions.Count != 3)
                throw new InvalidOperationException($"Gemini devolvió {missions.Count} misiones en vez de 3.");

            using var conn = _factory.Create();
            await conn.OpenAsync(cancellationToken);

            foreach (var mission in missions)
            {
                var sql = @"
                    INSERT INTO DB2INST1.MISION
                    (TITULO, DESCRIPCION, TIPO, PUNTOS)
                    SELECT @titulo, @descripcion, @tipo, @puntos
                    FROM SYSIBM.SYSDUMMY1
                    WHERE NOT EXISTS (
                        SELECT 1
                        FROM DB2INST1.MISION
                        WHERE TITULO = @titulo
                          AND TIPO = @tipo
                          AND PUNTOS = @puntos
                    )";

                using var cmd = new DB2Command(sql, conn);
                cmd.Parameters.Add(new DB2Parameter("@titulo", mission.Titulo));
                cmd.Parameters.Add(new DB2Parameter("@descripcion", mission.Descripcion));
                cmd.Parameters.Add(new DB2Parameter("@tipo", mission.Tipo));
                cmd.Parameters.Add(new DB2Parameter("@puntos", mission.Puntos));

                await cmd.ExecuteNonQueryAsync(cancellationToken);
            }

            _logger.LogInformation("Se generaron 3 misiones nuevas con Gemini.");
        }

        private static string ExtractGeminiText(string responseJson)
        {
            using var doc = JsonDocument.Parse(responseJson);

            if (!doc.RootElement.TryGetProperty("candidates", out var candidates) ||
                candidates.GetArrayLength() == 0)
            {
                throw new InvalidOperationException("Gemini no devolvió candidates.");
            }

            var firstCandidate = candidates[0];

            if (!firstCandidate.TryGetProperty("content", out var content) ||
                !content.TryGetProperty("parts", out var parts) ||
                parts.GetArrayLength() == 0)
            {
                throw new InvalidOperationException("Gemini no devolvió contenido utilizable.");
            }

            var firstPart = parts[0];

            if (!firstPart.TryGetProperty("text", out var textElement))
                throw new InvalidOperationException("Gemini no devolvió texto en la primera parte.");

            return textElement.GetString() ?? "";
        }

        private static List<GeminiMissionDto> ParseMissions(string rawText)
        {
            var normalized = rawText.Trim();

            if (normalized.StartsWith("```"))
            {
                normalized = normalized
                    .Replace("```json", "", StringComparison.OrdinalIgnoreCase)
                    .Replace("```", "")
                    .Trim();
            }

            var start = normalized.IndexOf('[');
            var end = normalized.LastIndexOf(']');

            if (start >= 0 && end > start)
            {
                normalized = normalized.Substring(start, end - start + 1);
            }

            var missions = JsonSerializer.Deserialize<List<GeminiMissionDto>>(
                normalized,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            return missions ?? new List<GeminiMissionDto>();
        }
    }
}