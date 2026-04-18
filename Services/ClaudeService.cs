using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using LotteryCrawler.Interface;

namespace LotteryCrawler.Services
{
    public class ClaudeOptions
    {
        public string ApiKey { get; set; } = string.Empty;
        public string Version { get; set; } = "2024-12-01";
        public string BaseUrl { get; set; } = "https://api.anthropic.com";
        public string RequestPath { get; set; } = "/v1/messages";
        public int TimeoutSeconds { get; set; } = 30;
    }

    public class ClaudeService : IClaudeService
    {
        private readonly HttpClient _httpClient;
        private readonly ClaudeOptions _options;

        public ClaudeService(HttpClient httpClient, IOptions<ClaudeOptions> options)
        {
            _httpClient = httpClient;
            _options = options.Value;

            // Environment fallback for ApiKey
            if (string.IsNullOrWhiteSpace(_options.ApiKey))
            {
                var envKey = Environment.GetEnvironmentVariable("CLAUDE_API_KEY") ?? Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
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

                if (!_httpClient.DefaultRequestHeaders.Contains("x-api-key"))
                    _httpClient.DefaultRequestHeaders.Add("x-api-key", _options.ApiKey);

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
                return "Claude ApiKey is not configured. Set Claude:ApiKey in appsettings or via environment.";
            }

            // Basic integration with Claude (Anthropic-like) assuming a JSON body with `model` and `prompt`.
            var body = new
            {
                model = _options.Version,
                prompt = prompt,
                max_tokens = 800
            };

            var json = JsonSerializer.Serialize(body);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Build absolute URL using configured RequestPath in case BaseAddress was not set on HttpClient
            var path = _options.RequestPath ?? "/v1/messages";
            var endpoint = _options.BaseUrl?.TrimEnd('/') + path;
            if (string.IsNullOrWhiteSpace(endpoint))
                throw new InvalidOperationException("Claude BaseUrl is not configured.");

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
                return $"Claude returned {(int)resp.StatusCode} {resp.ReasonPhrase}: {respText}";
            }

            // Try to parse a simple text field; fall back to raw response
            try
            {
                using var doc = JsonDocument.Parse(respText);
                if (doc.RootElement.TryGetProperty("output", out var outEl))
                {
                    return outEl.ToString();
                }
                if (doc.RootElement.TryGetProperty("completion", out var comp))
                {
                    return comp.GetString() ?? respText;
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
