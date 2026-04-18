using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using LotteryCrawler.Interface;

namespace LotteryCrawler.Services
{
    public class OpenAIOptions
    {
        public string ApiKey { get; set; } = string.Empty;
        public string Model { get; set; } = "gpt-4-1106-preview";
        public string BaseUrl { get; set; } = "https://api.openai.com";
        public string RequestPath { get; set; } = "/v1/chat/completions";
        public int TimeoutSeconds { get; set; } = 30;
        public float Temperature { get; set; } = 0.7f;
        public int MaxTokens { get; set; } = 800;
    }

    public class OpenAIService : IOpenAIService
    {
        private readonly HttpClient _httpClient;
        private readonly OpenAIOptions _options;

        public OpenAIService(HttpClient httpClient, IOptions<OpenAIOptions> options)
        {
            _httpClient = httpClient;
            _options = options.Value;

            // Environment fallback for ApiKey
            if (string.IsNullOrWhiteSpace(_options.ApiKey))
            { 
                var envKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
                if (!string.IsNullOrWhiteSpace(envKey))
                {
                    _options.ApiKey = envKey;
                }
            }

            // Configure HttpClient headers if an API key is present
            if (!string.IsNullOrWhiteSpace(_options.ApiKey))
            {
                if (_httpClient.DefaultRequestHeaders.Authorization == null)
                    _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);

                if (!_httpClient.DefaultRequestHeaders.Accept.Any(h => h.MediaType == "application/json"))
                    _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            }

            // Configure HttpClient timeout
            _httpClient.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);
        }

        public async Task<string> SendAsync(string prompt)
        {
            if (string.IsNullOrWhiteSpace(_options.ApiKey))
            {
                return "OpenAI ApiKey is not configured. Set OpenAI:ApiKey in appsettings or OPENAI_API_KEY environment variable.";
            }

            var messages = new[]
            {
                new { role = "user", content = prompt }
            };

            var body = new
            {
                model = _options.Model,
                messages,
                temperature = _options.Temperature,
                max_tokens = _options.MaxTokens
            };

            var json = JsonSerializer.Serialize(body);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            var endpoint = _options.BaseUrl?.TrimEnd('/') + _options.RequestPath;
            if (string.IsNullOrWhiteSpace(endpoint))
                throw new InvalidOperationException("OpenAI BaseUrl is not configured.");

            HttpResponseMessage resp;
            try
            {
                resp = await _httpClient.PostAsync(endpoint, content);
            }
            catch (Exception ex)
            {
                return $"Request failed: {ex.Message}";
            }

            var respText = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
            {
                return $"OpenAI returned {(int)resp.StatusCode} {resp.ReasonPhrase}: {respText}";
            }

            // Parse the OpenAI chat completion response
            try
            {
                using var doc = JsonDocument.Parse(respText);
                var choices = doc.RootElement.GetProperty("choices");
                if (choices.GetArrayLength() > 0)
                {
                    var firstChoice = choices[0];
                    if (firstChoice.TryGetProperty("message", out var message) &&
                        message.TryGetProperty("content", out var messageContent))
                    {
                        return messageContent.GetString() ?? respText;
                    }
                }
            }
            catch
            {
                // ignore parse errors
            }

            return respText;
        }
    }
}