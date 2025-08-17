using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace _10xCards.backend.Domain;

public class GenerationSession
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();
    public string UserId { get; set; } = null!;
    public string InputTextHash { get; set; } = null!; // To deduplicate
    public int ProposedCount { get; set; }
    public int AcceptedCount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
