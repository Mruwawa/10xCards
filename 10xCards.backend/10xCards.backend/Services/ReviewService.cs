using _10xCards.backend.Domain;
using _10xCards.backend.Infrastructure;
using MongoDB.Driver;

namespace _10xCards.backend.Services;

public class ReviewService
{
    private readonly MongoContext _db;
    public ReviewService(MongoContext db) { _db = db; }

    public async Task<Flashcard?> GetNextDueAsync(string userId, DateTime? now = null, string? excludeFlashcardId = null)
    {
        var ts = now ?? DateTime.UtcNow;
        var filter = Builders<Flashcard>.Filter.Where(f => f.UserId == userId && (f.NextReview == null || f.NextReview <= ts));
        if (!string.IsNullOrEmpty(excludeFlashcardId))
        {
            filter &= Builders<Flashcard>.Filter.Ne(f => f.Id, excludeFlashcardId);
        }
        return await _db.Flashcards.Find(filter)
            .SortBy(f => f.NextReview)
            .FirstOrDefaultAsync();
    }

    public async Task<Flashcard?> ReviewAsync(string userId, string flashcardId, int quality)
    {
        if (quality < 0 || quality > 5) throw new ArgumentOutOfRangeException(nameof(quality), "Quality must be 0-5");
        var card = await _db.Flashcards.Find(f => f.Id == flashcardId && f.UserId == userId).FirstOrDefaultAsync();
        if (card == null) return null;

        // SM-2 simplified
        double ef = card.EaseFactor <= 0 ? 2.5 : card.EaseFactor;
        if (quality < 3)
        {
            // Immediate relearning style: very short retry delays
            card.Repetitions = 0; // reset learning phase
            // Map quality 0,1,2 to short minute delays (or seconds for 0)
            var minutes = quality switch
            {
                0 => 0.05, // ~3s
                1 => 0.5,  // 30s
                2 => 1.0,  // 1 min
                _ => 0.5
            };
            card.IntervalDays = 0; // conceptually still today
            card.NextReview = DateTime.UtcNow.AddMinutes(minutes);
        }
        else
        {
            if (card.Repetitions == 0)
                card.IntervalDays = 1;
            else if (card.Repetitions == 1)
                card.IntervalDays = 6;
            else
                card.IntervalDays = (int)Math.Round(card.IntervalDays * ef);
            card.Repetitions += 1;
            // Update EF (standard SM-2 adjustment)
            ef = ef + (0.1 - (5 - quality) * (0.08 + (5 - quality) * 0.02));
            if (ef < 1.3) ef = 1.3;
            card.EaseFactor = ef;
            card.NextReview = DateTime.UtcNow.AddDays(card.IntervalDays);
        }
        // Ensure EaseFactor stored even for low quality (unchanged except clamp)
        if (quality < 3)
        {
            // Light penalty for failures
            ef = ef - 0.2;
            if (ef < 1.3) ef = 1.3;
            card.EaseFactor = ef;
        }
        card.UpdatedAt = DateTime.UtcNow;

        // Persist changes
        var update = Builders<Flashcard>.Update
            .Set(c => c.IntervalDays, card.IntervalDays)
            .Set(c => c.EaseFactor, card.EaseFactor)
            .Set(c => c.Repetitions, card.Repetitions)
            .Set(c => c.NextReview, card.NextReview)
            .Set(c => c.UpdatedAt, card.UpdatedAt);
        await _db.Flashcards.UpdateOneAsync(f => f.Id == card.Id && f.UserId == userId, update);

        // Log review
        var log = new ReviewLog { UserId = userId, FlashcardId = card.Id, Quality = quality };
        await _db.ReviewLogs.InsertOneAsync(log);

        return card;
    }
}
