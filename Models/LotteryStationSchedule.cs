using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace LotteryCrawler.Models
{
    public class LotteryStationSchedule
    {
        [BsonId]
        public ObjectId Id { get; set; }

        [BsonElement("dayOfWeek")]
        public string DayOfWeek { get; set; } = string.Empty;

        [BsonElement("stations")]
        public List<ObjectId> Stations { get; set; } = new();
    }

    public class Province
    {
        [BsonId]
        public ObjectId Id { get; set; }

        [BsonElement("name")]
        public string Name { get; set; } = string.Empty;
    }
}
