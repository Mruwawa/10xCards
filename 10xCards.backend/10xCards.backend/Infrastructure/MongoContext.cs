using MongoDB.Driver;
using _10xCards.backend.Domain;

namespace _10xCards.backend.Infrastructure;

public class MongoContext
{
    public IMongoDatabase Database { get; }
    public IMongoCollection<User> Users => Database.GetCollection<User>("users");
    public IMongoCollection<Flashcard> Flashcards => Database.GetCollection<Flashcard>("flashcards");
    public IMongoCollection<GenerationSession> GenerationSessions => Database.GetCollection<GenerationSession>("generationSessions");
    public IMongoCollection<ReviewLog> ReviewLogs => Database.GetCollection<ReviewLog>("reviewLogs");

    public MongoContext(MongoSettings settings)
    {
        var client = new MongoClient(settings.ConnectionString);
        Database = client.GetDatabase(settings.Database);
        EnsureIndexes();
    }

    private void EnsureIndexes()
    {
        // Users: unique email
        var userIndexKeys = Builders<User>.IndexKeys.Ascending(u => u.Email);
        var userIndexModel = new CreateIndexModel<User>(userIndexKeys, new CreateIndexOptions { Unique = true, Name = "ux_users_email" });
        Users.Indexes.CreateOne(userIndexModel);

        // Flashcards: (UserId, NextReview) for due queries
        var flashcardDueKeys = Builders<Flashcard>.IndexKeys.Ascending(f => f.UserId).Ascending(f => f.NextReview);
        Flashcards.Indexes.CreateOne(new CreateIndexModel<Flashcard>(flashcardDueKeys, new CreateIndexOptions { Name = "ix_flashcards_user_nextReview" }));

        // Flashcards: (UserId, CreatedAt) for listing / stats
        var flashcardCreatedKeys = Builders<Flashcard>.IndexKeys.Ascending(f => f.UserId).Descending(f => f.CreatedAt);
        Flashcards.Indexes.CreateOne(new CreateIndexModel<Flashcard>(flashcardCreatedKeys, new CreateIndexOptions { Name = "ix_flashcards_user_createdAt" }));
    }
}
