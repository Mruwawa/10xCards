using _10xCards.backend.Infrastructure;
using _10xCards.backend.Services;
using _10xCards.backend.Domain;
using FluentAssertions;
using Xunit;

namespace _10xCards.backend.Tests;

public class ReviewServiceTests : IClassFixture<MongoFixture>
{
    private readonly MongoFixture _fx;
    public ReviewServiceTests(MongoFixture fx) => _fx = fx;

    private (ReviewService review, MongoContext db, string userId, Flashcard card) SeedOne()
    {
        var db = new MongoContext(_fx.Settings);
        var review = new ReviewService(db);
        var user = new User { Email = $"u{Guid.NewGuid()}@ex.com", PasswordHash = "x" };
        db.Users.InsertOne(user);
        var card = new Flashcard { UserId = user.Id, Front = "F", Back = "B", CreatedAt = DateTime.UtcNow, NextReview = DateTime.UtcNow.AddMinutes(-1), IntervalDays = 0, EaseFactor = 2.5, Repetitions = 0 };
        db.Flashcards.InsertOne(card);
        return (review, db, user.Id, card);
    }

    [Fact]
    public async Task GetNextDueAsync_ReturnsCard()
    {
        var (review, _, userId, card) = SeedOne();
        var next = await review.GetNextDueAsync(userId, null, null);
        next!.Id.Should().Be(card.Id);
    }

    [Fact]
    public async Task ReviewAsync_LowQuality_ShortInterval()
    {
        var (review, db, userId, card) = SeedOne();
        var updated = await review.ReviewAsync(userId, card.Id, 1);
        updated.Should().NotBeNull();
        (updated!.NextReview - DateTime.UtcNow).Value.TotalMinutes.Should().BeLessThan(10);
    }

    [Fact]
    public async Task ReviewAsync_HighQuality_IncreasesInterval()
    {
        var (review, db, userId, card) = SeedOne();
        var updated = await review.ReviewAsync(userId, card.Id, 5);
        updated!.IntervalDays.Should().BeGreaterThan(card.IntervalDays);
        updated.Repetitions.Should().Be(card.Repetitions + 1);
    }
}
