namespace LotteryCrawler.Services
{
    public class MongoDBSettings
    {
        public string ConnectionString { get; set; } = string.Empty;
        public string DatabaseName { get; set; } = string.Empty;
        public string LotteryCollectionName { get; set; } = "LotteryResult";
        public string PredictionCollectionName { get; set; } = "PredictionResult";
        public string ProvinceCollectionName { get; set; } = "Province";
        public string LotteryStationCollectionName { get; set; } = "Lotterystation";
    }
}
