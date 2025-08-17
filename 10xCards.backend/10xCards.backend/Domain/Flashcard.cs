using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace _10xCards.backend.Domain;

public class Flashcard
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();
    public string UserId { get; set; } = null!;
    public string Front { get; set; } = null!;
    public string Back { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public FlashcardSource Source { get; set; } = FlashcardSource.Manual;
    // Spaced repetition scheduling fields (simplified)
    public int IntervalDays { get; set; } = 0;
    public double EaseFactor { get; set; } = 2.5;
    public int Repetitions { get; set; } = 0;
    public DateTime? NextReview { get; set; }
}

public enum FlashcardSource
{
    Manual,
    AI
}
