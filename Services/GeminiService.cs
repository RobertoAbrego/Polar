using System.Text;
using System.Text.Json;

namespace Polar.Services
{
    public class GeminiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public GeminiService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            // Busca la clave en appsettings.Development.json
            _apiKey = configuration["GeminiSettings:ApiKey"] ?? throw new ArgumentNullException("Gemini API Key no configurada.");
        }

        public async Task<GeminiResponseDTO?> EvaluarPublicacionAsync(string contenidoPost)
        {
            // URL de Gemini 1.5 Flash
            string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={_apiKey}";

            // El prompt del sistema: Aquí le damos las reglas del juego a la IA
            string promptSistema = "Actúa como un moderador de foro y sistema de gamificación. " +
                                   "Analiza la publicación del usuario. Si está vacía, contiene solo espacios, " +
                                   "o es texto basura sin sentido (spam), asígnale 0 puntos. " +
                                   "Si tiene contenido real, evalúa su calidad y asígnale de 1 a 10 puntos. " +
                                   "Genera también una respuesta corta, amigable y natural para el usuario. " +
                                   "Debes responder ESTRICTAMENTE en formato JSON con la estructura: " +
                                   "{\"puntos\": X, \"respuesta\": \"texto\"}. No agregues markdown ni texto extra fuera del JSON.";

            // Estructura del JSON que Google pide
            var requestBody = new
            {
                contents = new[]
                {
                    new { parts = new[] { new { text = $"{promptSistema}\n\nPublicación del usuario: \"{contenidoPost}\"" } } }
                }
            };

            var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            string contentString = JsonSerializer.Serialize(requestBody, jsonOptions);
            var content = new StringContent(contentString, Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.PostAsync(url, content);
                if (!response.IsSuccessStatusCode) return null;

                string responseString = await response.Content.ReadAsStringAsync();
                
                // Parsear la respuesta nativa de Google para extraer el texto
                using var doc = JsonDocument.Parse(responseString);
                string? textJson = doc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetString();

                if (string.IsNullOrEmpty(textJson)) return null;

                // Limpiar posibles bloques de código markdown si la IA ignora la instrucción de no ponerlos
                textJson = textJson.Replace("```json", "").Replace("```", "").Trim();

                // Convertir el JSON de la IA a nuestro objeto C#
                return JsonSerializer.Deserialize<GeminiResponseDTO>(textJson, jsonOptions);
            }
            catch
            {
                // Si algo falla (red, API caída), devolvemos null para que el backend no se caiga
                return null;
            }
        }
    }

    // Objeto temporal para mapear lo que responde la IA
    public class GeminiResponseDTO
    {
        public int Puntos { get; set; }
        public string Respuesta { get; set; } = string.Empty;
    }
}