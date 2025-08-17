using _10xCards.backend.Infrastructure;
using MongoDB.Driver;

namespace _10xCards.backend.Services;

public class StatsService
{
    private readonly MongoContext _db;
    public StatsService(MongoContext db) { _db = db; }

    public async Task<GenerationStats> GetGenerationStatsAsync(string userId, CancellationToken ct = default)
    {
        // Aggregate generation sessions
        var sessions = await _db.GenerationSessions
            .Find(s => s.UserId == userId)
            .Project(s => new { s.ProposedCount, s.AcceptedCount })
            .ToListAsync(ct);
        long totalSessions = sessions.Count;
        long totalProposed = sessions.Sum(s => (long)s.ProposedCount);
        long totalAccepted = sessions.Sum(s => (long)s.AcceptedCount);
        double acceptanceRate = totalProposed == 0 ? 0 : (double)totalAccepted / totalProposed;

        // Flashcards counts
        var totalFlashcards = await _db.Flashcards.CountDocumentsAsync(f => f.UserId == userId, cancellationToken: ct);
        var aiFlashcards = await _db.Flashcards.CountDocumentsAsync(f => f.UserId == userId && f.Source == Domain.FlashcardSource.AI, cancellationToken: ct);
        var manualFlashcards = totalFlashcards - aiFlashcards;
        double aiUsageRate = totalFlashcards == 0 ? 0 : (double)aiFlashcards / totalFlashcards;

        return new GenerationStats(totalSessions, totalProposed, totalAccepted, acceptanceRate, totalFlashcards, aiFlashcards, manualFlashcards, aiUsageRate);
    }

    public async Task<StudyDailyStats> GetTodayStudyStatsAsync(string userId, CancellationToken ct = default)
    {
        var utcNow = DateTime.UtcNow;
        var start = new DateTime(utcNow.Year, utcNow.Month, utcNow.Day, 0,0,0, DateTimeKind.Utc);
        var end = start.AddDays(1);
        var todayLogs = await _db.ReviewLogs.Find(r => r.UserId == userId && r.ReviewedAt >= start && r.ReviewedAt < end).ToListAsync(ct);
        int total = todayLogs.Count;
        int correct = todayLogs.Count(l => l.Quality >= 3);
        double accuracy = total == 0 ? 0 : (double)correct / total;
        var distribution = Enumerable.Range(0,6).Select(q => new StudyQualityBucket(q, todayLogs.Count(l => l.Quality == q))).ToList();
        return new StudyDailyStats(start, total, correct, accuracy, distribution);
    }
}

public record GenerationStats(
    long Sessions,
    long Proposed,
    long Accepted,
    double AcceptanceRate,
    long FlashcardsTotal,
    long FlashcardsAI,
    long FlashcardsManual,
    double AIUsageRate);

public record StudyQualityBucket(int Quality, int Count);
public record StudyDailyStats(DateTime DayUtc, int Total, int Correct, double Accuracy, List<StudyQualityBucket> Distribution);
