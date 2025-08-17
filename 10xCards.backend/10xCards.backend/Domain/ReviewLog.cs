using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace _10xCards.backend.Domain;

public class ReviewLog
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();
    public string UserId { get; set; } = null!;
    public string FlashcardId { get; set; } = null!;
    public DateTime ReviewedAt { get; set; } = DateTime.UtcNow;
    public int Quality { get; set; } // 0-5 rating
}
