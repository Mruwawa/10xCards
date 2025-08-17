using System.IdentityModel.Tokens.Jwt;
using _10xCards.backend.Domain;
using _10xCards.backend.Infrastructure;
using _10xCards.backend.Services;
using FluentAssertions;
using Mongo2Go;
using MongoDB.Driver;
using Xunit;

namespace _10xCards.backend.Tests;

public class AuthServiceTests : IClassFixture<MongoFixture>
{
    private readonly MongoFixture _fixture;
    public AuthServiceTests(MongoFixture fixture) => _fixture = fixture;

    private AuthService CreateService() => new AuthService(new MongoContext(_fixture.Settings), _fixture.Configuration);

    [Fact]
    public async Task Register_NewEmail_CreatesUser()
    {
        var svc = CreateService();
        var user = await svc.RegisterAsync("test@example.com", "Password123!");
        user.Should().NotBeNull();
        user!.Id.Should().NotBeNullOrEmpty();
        var valid = await svc.ValidateUserAsync("test@example.com", "Password123!");
        valid.Should().NotBeNull();
    }

    [Fact]
    public async Task Register_DuplicateEmail_ReturnsNull()
    {
        var svc = CreateService();
        await svc.RegisterAsync("dup@example.com", "x");
        var again = await svc.RegisterAsync("dup@example.com", "y");
        again.Should().BeNull();
    }

    [Fact]
    public async Task GenerateJwt_ContainsUserClaims()
    {
        var svc = CreateService();
        var user = await svc.RegisterAsync("claims@example.com", "x");
        var tokenString = svc.GenerateJwt(user!);
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(tokenString);
        token.Claims.Should().Contain(c => c.Type == System.Security.Claims.ClaimTypes.Email && c.Value == "claims@example.com");
        token.Claims.Should().Contain(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier && c.Value == user!.Id);
    }
}
