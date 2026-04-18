using LotteryCrawler.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace LotteryCrawler.Services
{
    public class MongoDBService
    {
        private readonly IMongoCollection<LotteryResult> _lotteryCollection;
        private readonly IMongoCollection<LotteryResult> _predictionCollection;
        private readonly IMongoCollection<Province> _provinceCollection;
        private readonly IMongoCollection<LotteryStationSchedule> _lotteryStationCollection;

        public MongoDBService(IOptions<MongoDBSettings> settings)
        {
            var client = new MongoClient(settings.Value.ConnectionString);
            var database = client.GetDatabase(settings.Value.DatabaseName);

            _lotteryCollection = database.GetCollection<LotteryResult>(settings.Value.LotteryCollectionName);
            _predictionCollection = database.GetCollection<LotteryResult>(settings.Value.PredictionCollectionName);
            _provinceCollection = database.GetCollection<Province>(settings.Value.ProvinceCollectionName);
            _lotteryStationCollection = database.GetCollection<LotteryStationSchedule>(settings.Value.LotteryStationCollectionName);
        }

        // Save LotteryResult
        public async Task SaveLotteryResultAsync(LotteryResult result)
        {
            await _lotteryCollection.InsertOneAsync(result);
        }

        // Save PredictionResult
        public async Task SavePredictionResultAsync(LotteryResult prediction)
        {
            await _predictionCollection.InsertOneAsync(prediction);
        }

        // Get tomorrow's lottery stations
        public async Task<List<Province>> GetTomorrowStationsAsync()
        {
            // Get next day of week in Vietnamese
            var nextDay = DateTime.Now.AddDays(1);
            string nextDayOfWeek = GetVietnameseDayOfWeek(nextDay.DayOfWeek);

            // Get schedule for next day
            var schedule = await _lotteryStationCollection
                .Find(x => x.DayOfWeek == nextDayOfWeek)
                .FirstOrDefaultAsync();

            if (schedule == null)
            {
                Console.WriteLine($"❌ Không tìm thấy lịch quay cho {nextDayOfWeek}");
                return new List<Province>();
            }

            // Get information of provinces by Id list
            var filter = Builders<Province>.Filter.In(p => p.Id, schedule.Stations);
            var provinces = await _provinceCollection.Find(filter).ToListAsync();

            Console.WriteLine($"✅ Ngày mai ({nextDayOfWeek}) quay các đài:");
            foreach (var p in provinces)
            {
                Console.WriteLine($"- {p.Name}");
            }

            return provinces;
        }

        private string GetVietnameseDayOfWeek(DayOfWeek day)
        {
            return day switch
            {
                DayOfWeek.Monday => "Thứ 2",
                DayOfWeek.Tuesday => "Thứ 3",
                DayOfWeek.Wednesday => "Thứ 4",
                DayOfWeek.Thursday => "Thứ 5",
                DayOfWeek.Friday => "Thứ 6",
                DayOfWeek.Saturday => "Thứ 7",
                DayOfWeek.Sunday => "Chủ nhật",
                _ => ""
            };
        }
    }
}
