using LotteryCrawler.Models;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace LotteryCrawler.Services
{
    public class LotteryPredictionService
    {
        private readonly string resultDir = "result";
        private readonly MongoDBService _mongoDBService;

        public LotteryPredictionService(MongoDBService mongoDBService)
        {
            _mongoDBService = mongoDBService;
        }

        public async Task RunPredictionAsync()
        {
            var nextDayProvinces = await _mongoDBService.GetTomorrowStationsAsync();
            if (nextDayProvinces == null || nextDayProvinces.Count == 0)
            {
                Console.WriteLine("❌ Không tìm thấy đài quay cho ngày mai.");
                return;
            }

            var nextDayStations = nextDayProvinces.Select(p => p.Name).ToList();
            var history = new List<LotteryResult>();

            for (int i = 0; i < 30; i++)
            {
                string elementId = $"MN{i}";
                string filePath = Path.Combine(resultDir, $"lottery-draw-{elementId}.json");

                if (!File.Exists(filePath))
                {
                    Console.WriteLine($"⚠️ Không tìm thấy file: {filePath}");
                    continue;
                }

                try
                {
                    string json = await File.ReadAllTextAsync(filePath);
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var lotteryResult = JsonSerializer.Deserialize<LotteryResult>(json, options);

                    if (lotteryResult?.Prizes == null) continue;

                    var filteredPrizes = lotteryResult.Prizes
                        .Where(p => nextDayStations.Any(st =>
                            p.Province.Contains(st, StringComparison.OrdinalIgnoreCase)))
                        .ToList();

                    if (filteredPrizes.Count > 0)
                    {
                        history.Add(new LotteryResult
                        {
                            Date = lotteryResult.Date,
                            Region = lotteryResult.Region,
                            Prizes = filteredPrizes
                        });
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Lỗi khi đọc file {filePath}: {ex.Message}");
                }
            }

            if (history.Count == 0)
            {
                Console.WriteLine("⚠️ Không có dữ liệu lịch sử nào khớp với các đài ngày mai.");
                return;
            }

            string nextDayStr = DateTime.Now.AddDays(1).ToString("yyyy-MM-dd");

            var promptTemplate = new
            {
                date = nextDayStr,
                region = "Miền Nam",
                prizes = nextDayStations.Select(station => new
                {
                    province = station,
                    data = new[]
                    {
                new {
                    g8 = new [] { "xx" },
                    g7 = new [] { "xxx" },
                    g6 = new [] { "xxxx", "xxxx", "xxxx" },
                    g5 = new [] { "xxxx" },
                    g4 = new [] { "xxxxx","xxxxx","xxxxx","xxxxx","xxxxx","xxxxx","xxxxx" },
                    g3 = new [] { "xxxxx","xxxxx" },
                    g2 = new [] { "xxxxx" },
                    g1 = new [] { "xxxxx" },
                    db = new [] { "xxxxxx" }
                }
            }
                }).ToList()
            };

            string promptJson = JsonSerializer.Serialize(promptTemplate, new JsonSerializerOptions { WriteIndented = true });

            Console.WriteLine("🧠 Prompt gửi đến AI:");
            Console.WriteLine(promptJson);

            var apiKey = "";
            var http = new HttpClient();
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            var body = new
            {
                model = "gpt-5-nano",
                messages = new[]
                {
            new { role = "system", content = "You are an AI lottery prediction model. Output valid JSON prediction." },
            new { role = "user", content = $"Analyze the 30-day history and predict tomorrow's results based on this data:\n\n{JsonSerializer.Serialize(history)}\n\nGenerate prediction for:\n{promptJson}" }
        }
            };

            string requestBody = JsonSerializer.Serialize(body);
            var response = await http.PostAsync("https://api.openai.com/v1/chat/completions",
                new StringContent(requestBody, Encoding.UTF8, "application/json"));

            string responseText = await response.Content.ReadAsStringAsync();
            Console.WriteLine("🧾 AI Response:");
            Console.WriteLine(responseText);
        }
    }
}