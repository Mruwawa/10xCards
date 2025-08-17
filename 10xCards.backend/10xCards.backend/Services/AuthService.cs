using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using _10xCards.backend.Domain;
using _10xCards.backend.Infrastructure;
using BCrypt.Net;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;

namespace _10xCards.backend.Services;

public class AuthService
{
    private readonly MongoContext _db;
    private readonly IConfiguration _config;

    public AuthService(MongoContext db, IConfiguration config)
    {
        _db = db; _config = config;
    }

    public async Task<User?> RegisterAsync(string email, string password)
    {
        var existing = await _db.Users.Find(u => u.Email == email).FirstOrDefaultAsync();
        if (existing != null) return null;
        var user = new User { Email = email, PasswordHash = BCrypt.Net.BCrypt.HashPassword(password) };
        await _db.Users.InsertOneAsync(user);
        return user;
    }

    public async Task<User?> ValidateUserAsync(string email, string password)
    {
        var user = await _db.Users.Find(u => u.Email == email).FirstOrDefaultAsync();
        if (user == null) return null;
        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash)) return null;
        return user;
    }

    public string GenerateJwt(User user)
    {
    var keyString = _config["Jwt:Key"] ?? "dev_fallback_key_change_me_super_long_32b_min_secret";
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyString));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var hasIssuer = !string.IsNullOrWhiteSpace(_config["Jwt:Issuer"]);
        var hasAudience = !string.IsNullOrWhiteSpace(_config["Jwt:Audience"]);
        var token = new JwtSecurityToken(
            issuer: hasIssuer ? _config["Jwt:Issuer"] : null,
            audience: hasAudience ? _config["Jwt:Audience"] : null,
            claims: new[] { new Claim(ClaimTypes.NameIdentifier, user.Id), new Claim(ClaimTypes.Email, user.Email) },
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
