using _10xCards.backend.Domain;
using _10xCards.backend.Infrastructure;
using _10xCards.backend.Services;
using FluentAssertions;
using Xunit;

namespace _10xCards.backend.Tests;

public class StatsServiceTests : IClassFixture<MongoFixture>
{
    private readonly MongoFixture _fx;
    public StatsServiceTests(MongoFixture fx) => _fx = fx;

    private (StatsService stats, MongoContext db, string userId) Seed()
    {
        var db = new MongoContext(_fx.Settings);
        var user = new User { Email = $"s{Guid.NewGuid()}@ex.com", PasswordHash = "h" };
        db.Users.InsertOne(user);
        return (new StatsService(db), db, user.Id);
    }

    [Fact]
    public async Task GetGenerationStatsAsync_Empty_ReturnsZeros()
    {
        var (stats, db, userId) = Seed();
    var gs = await stats.GetGenerationStatsAsync(userId);
    gs.Sessions.Should().Be(0);
    gs.Proposed.Should().Be(0);
    gs.Accepted.Should().Be(0);
    gs.FlashcardsTotal.Should().Be(0);
    }
}
