using _10xCards.backend.Domain;
using _10xCards.backend.Infrastructure;
using MongoDB.Driver;

namespace _10xCards.backend.Services;

public class AccountService
{
    private readonly MongoContext _db;
    public AccountService(MongoContext db) { _db = db; }

    public async Task<AccountOverview?> GetOverviewAsync(string userId)
    {
        var user = await _db.Users.Find(u => u.Id == userId).FirstOrDefaultAsync();
        if (user == null) return null;
        var total = await _db.Flashcards.CountDocumentsAsync(f => f.UserId == userId);
        var ai = await _db.Flashcards.CountDocumentsAsync(f => f.UserId == userId && f.Source == FlashcardSource.AI);
        var manual = total - ai;
        return new AccountOverview(user.Id, user.Email, user.CreatedAt, total, ai, manual);
    }

    public async Task<bool> DeleteAccountAsync(string userId)
    {
        // Delete all related data in parallel where possible
        var userDelete = _db.Users.DeleteOneAsync(u => u.Id == userId);
        var flashDelete = _db.Flashcards.DeleteManyAsync(f => f.UserId == userId);
        var genDelete = _db.GenerationSessions.DeleteManyAsync(s => s.UserId == userId);
        var reviewDelete = _db.ReviewLogs.DeleteManyAsync(r => r.UserId == userId);
        await Task.WhenAll(userDelete, flashDelete, genDelete, reviewDelete);
        return userDelete.Result.DeletedCount > 0;
    }
}

public record AccountOverview(string Id, string Email, DateTime CreatedAt, long FlashcardsTotal, long FlashcardsAI, long FlashcardsManual);
