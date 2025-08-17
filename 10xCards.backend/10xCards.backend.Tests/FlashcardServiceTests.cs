using _10xCards.backend.Infrastructure;
using _10xCards.backend.Domain;
using _10xCards.backend.Services;
using FluentAssertions;
using Xunit;

namespace _10xCards.backend.Tests;

public class FlashcardServiceTests : IClassFixture<MongoFixture>
{
    private readonly MongoFixture _fx;
    public FlashcardServiceTests(MongoFixture fx) => _fx = fx;

    private (FlashcardService svc, MongoContext db, string userId) CreateSvc()
    {
        var db = new MongoContext(_fx.Settings);
        var user = new User { Email = $"u{Guid.NewGuid()}@ex.com", PasswordHash = "h" };
        db.Users.InsertOne(user);
        return (new FlashcardService(db), db, user.Id);
    }

    [Fact]
    public async Task CreateAsync_Valid_SetsFields()
    {
        var (svc, db, userId) = CreateSvc();
        var card = await svc.CreateAsync(userId, "Front", "Back", FlashcardSource.Manual);
        card.UserId.Should().Be(userId);
        card.Front.Should().Be("Front");
    }

    [Fact]
    public async Task UpdateAsync_OtherUser_ReturnsNull()
    {
        var (svc, db, userId) = CreateSvc();
        var card = await svc.CreateAsync(userId, "A", "B", FlashcardSource.Manual);
        var other = new User { Email = "o@x.com", PasswordHash = "x" }; db.Users.InsertOne(other);
        var updated = await svc.UpdateAsync(other.Id, card.Id, "X", "Y");
        updated.Should().BeNull();
    }

    [Fact]
    public async Task ResetSchedulingAsync_SetsNextReviewNearNow()
    {
        var (svc, db, userId) = CreateSvc();
        var card = await svc.CreateAsync(userId, "A", "B", FlashcardSource.Manual);
        await Task.Delay(10);
        var reset = await svc.ResetSchedulingAsync(userId, card.Id);
        (reset!.NextReview - DateTime.UtcNow).Value.TotalMinutes.Should().BeLessThan(1);
    }
}
