
using System.Security.Claims;
using _10xCards.backend.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi.Models;
using MongoDB.Driver;
using _10xCards.backend.Endpoints;

namespace _10xCards.backend
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Config & services
            builder.Services.Configure<MongoSettings>(builder.Configuration.GetSection("Mongo"));
            var mongoSettings = builder.Configuration.GetSection("Mongo").Get<MongoSettings>() ?? throw new InvalidOperationException("Mongo settings missing");
            builder.Services.AddSingleton(mongoSettings);
            builder.Services.AddSingleton<_10xCards.backend.Infrastructure.MongoContext>();
            builder.Services.AddScoped<_10xCards.backend.Services.AuthService>();
            builder.Services.AddScoped<_10xCards.backend.Services.FlashcardService>();
            builder.Services.AddScoped<_10xCards.backend.Services.GenerationService>();
            builder.Services.AddScoped<_10xCards.backend.Services.ReviewService>();
            builder.Services.AddScoped<_10xCards.backend.Services.AccountService>();
            builder.Services.AddScoped<_10xCards.backend.Services.StatsService>();
            // CORS (allow dev frontend)
            builder.Services.AddCors(options => options.AddDefaultPolicy(p => p.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin()));

            var jwtKey = builder.Configuration["Jwt:Key"] ?? "dev_fallback_key_change_me_super_long_32b_min_secret"; // fallback >=32 bytes
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = false, // relaxed for dev
                        ValidateAudience = false, // relaxed for dev
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                        ClockSkew = TimeSpan.FromMinutes(1)
                    };
                });
            builder.Services.AddAuthorization();
            builder.Services.AddOpenApi(); // minimal OpenAPI
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "10xCards API", Version = "v1" });
                var jwtScheme = new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Description = "Wpisz: Bearer <JWT>",
                    In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                    Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    Reference = new Microsoft.OpenApi.Models.OpenApiReference
                    {
                        Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                };
                c.AddSecurityDefinition("Bearer", jwtScheme);
                c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
                {
                    { jwtScheme, Array.Empty<string>() }
                });
            });

            var app = builder.Build();

            // Middleware pipeline
            app.UseCors();
            app.UseAuthentication();
            app.UseAuthorization();
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            // Simple in-memory rate limiting (na MVP) - login & generate
            var rateLimits = new Dictionary<string, List<DateTime>>();
            bool Allow(string key, int limit, TimeSpan window)
            {
                lock(rateLimits)
                {
                    if(!rateLimits.TryGetValue(key, out var list)) { list = new List<DateTime>(); rateLimits[key]=list; }
                    var cutoff = DateTime.UtcNow - window;
                    list.RemoveAll(t => t < cutoff);
                    if (list.Count >= limit) return false;
                    list.Add(DateTime.UtcNow);
                    return true;
                }
            }
            // Modular endpoint groups
            app.MapAuth(Allow);
            app.MapFlashcards(Allow);

            app.MapGet("/account", async (_10xCards.backend.Services.AccountService acc, ClaimsPrincipal user) =>
            {
                var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userId == null) return Results.Unauthorized();
                var overview = await acc.GetOverviewAsync(userId);
                return overview == null ? Results.NotFound() : Results.Ok(overview);
            }).RequireAuthorization();

            app.MapDelete("/account", async (_10xCards.backend.Services.AccountService acc, ClaimsPrincipal user) =>
            {
                var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userId == null) return Results.Unauthorized();
                var deleted = await acc.DeleteAccountAsync(userId);
                return deleted ? Results.NoContent() : Results.NotFound();
            }).RequireAuthorization();

            app.MapStats();
            app.MapStudy();

            app.Run();
        }
    }
}

public record RegisterRequest(string Email, string Password);
public record LoginRequest(string Email, string Password);
public record CreateFlashcardRequest(string Front, string Back);
public record UpdateFlashcardRequest(string Front, string Back);
public record GenerateFlashcardsRequest(string Text);
public record AcceptGeneratedRequest(string SessionId, List<AcceptGeneratedCardDto> Cards);
public record AcceptGeneratedCardDto(string Front, string Back);
public record StudyReviewRequest(string FlashcardId, int Quality);
public record BulkResetRequest(List<string> Ids);
