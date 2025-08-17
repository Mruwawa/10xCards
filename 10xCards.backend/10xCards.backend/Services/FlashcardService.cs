using _10xCards.backend.Domain;
using _10xCards.backend.Infrastructure;
using MongoDB.Driver;

namespace _10xCards.backend.Services;

public class FlashcardService
{
    private readonly MongoContext _db;
    public FlashcardService(MongoContext db) { _db = db; }

    public Task<List<Flashcard>> GetForUserAsync(string userId) => _db.Flashcards.Find(f => f.UserId == userId).ToListAsync();

    public async Task<Flashcard> CreateAsync(string userId, string front, string back, FlashcardSource source)
    {
    if (!IsValidSide(front) || !IsValidSide(back)) throw new ArgumentException("Front/back length must be 1-500 characters");
        var card = new Flashcard { UserId = userId, Front = front, Back = back, Source = source, NextReview = DateTime.UtcNow };
        await _db.Flashcards.InsertOneAsync(card);
        return card;
    }

    public async Task<Flashcard?> UpdateAsync(string userId, string id, string front, string back)
    {
    if (!IsValidSide(front) || !IsValidSide(back)) throw new ArgumentException("Front/back length must be 1-500 characters");
        var update = Builders<Flashcard>.Update.Set(f => f.Front, front).Set(f => f.Back, back).Set(f => f.UpdatedAt, DateTime.UtcNow);
        return await _db.Flashcards.FindOneAndUpdateAsync<Flashcard>(
            f => f.Id == id && f.UserId == userId,
            update,
            new FindOneAndUpdateOptions<Flashcard> { ReturnDocument = ReturnDocument.After },
            default
        );
    }

    public Task DeleteAsync(string userId, string id) => _db.Flashcards.DeleteOneAsync(f => f.Id == id && f.UserId == userId);

    public async Task<IReadOnlyList<Flashcard>> BulkCreateAiAsync(string userId, IEnumerable<(string front,string back)> items)
    {
        var now = DateTime.UtcNow;
        var docs = items
            .Where(i => IsValidSide(i.front) && IsValidSide(i.back))
            .Select(i => new Flashcard
            {
                UserId = userId,
                Front = i.front.Trim(),
                Back = i.back.Trim(),
                Source = FlashcardSource.AI,
                CreatedAt = now,
                NextReview = now
            })
            .ToList();
        if (docs.Count == 0) return Array.Empty<Flashcard>();
        await _db.Flashcards.InsertManyAsync(docs);
        return docs;
    }

    public async Task<Flashcard?> ResetSchedulingAsync(string userId, string id)
    {
        var update = Builders<Flashcard>.Update
            .Set(f => f.IntervalDays, 0)
            .Set(f => f.Repetitions, 0)
            .Set(f => f.EaseFactor, 2.5)
            .Set(f => f.NextReview, DateTime.UtcNow)
            .Set(f => f.UpdatedAt, DateTime.UtcNow);
        return await _db.Flashcards.FindOneAndUpdateAsync<Flashcard>(
            f => f.Id == id && f.UserId == userId,
            update,
            new FindOneAndUpdateOptions<Flashcard, Flashcard> { ReturnDocument = ReturnDocument.After }
        );
    }

    public async Task<long> ResetAllSchedulingAsync(string userId)
    {
        var update = Builders<Flashcard>.Update
            .Set(f => f.IntervalDays, 0)
            .Set(f => f.Repetitions, 0)
            .Set(f => f.EaseFactor, 2.5)
            .Set(f => f.NextReview, DateTime.UtcNow)
            .Set(f => f.UpdatedAt, DateTime.UtcNow);
        var res = await _db.Flashcards.UpdateManyAsync(f => f.UserId == userId, update);
        return res.ModifiedCount;
    }

    public async Task<long> ResetSchedulingForIdsAsync(string userId, IEnumerable<string> ids)
    {
        var idList = ids.Where(i => !string.IsNullOrWhiteSpace(i)).ToList();
        if (idList.Count == 0) return 0;
        var update = Builders<Flashcard>.Update
            .Set(f => f.IntervalDays, 0)
            .Set(f => f.Repetitions, 0)
            .Set(f => f.EaseFactor, 2.5)
            .Set(f => f.NextReview, DateTime.UtcNow)
            .Set(f => f.UpdatedAt, DateTime.UtcNow);
        var filter = Builders<Flashcard>.Filter.Where(f => f.UserId == userId && idList.Contains(f.Id));
        var res = await _db.Flashcards.UpdateManyAsync(filter, update);
        return res.ModifiedCount;
    }

    private static bool IsValidSide(string? s) => !string.IsNullOrWhiteSpace(s) && s.Trim().Length >= 1 && s.Trim().Length <= 500;
}
