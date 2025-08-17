using System.Security.Claims;
using _10xCards.backend.Services;
using _10xCards.backend.Infrastructure;
using MongoDB.Driver;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace _10xCards.backend.Endpoints;

public static class FlashcardsEndpoints
{
    public static IEndpointRouteBuilder MapFlashcards(this IEndpointRouteBuilder app, Func<string,int,TimeSpan,bool> allow)
    {
        app.MapGet("/flashcards", async (FlashcardService svc, ClaimsPrincipal user) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Results.Unauthorized();
            var list = await svc.GetForUserAsync(userId);
            return Results.Ok(list);
        }).RequireAuthorization();

        app.MapPost("/flashcards", async (FlashcardService svc, ClaimsPrincipal user, CreateFlashcardRequest req) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Results.Unauthorized();
            if (string.IsNullOrWhiteSpace(req.Front) || string.IsNullOrWhiteSpace(req.Back) || req.Front.Length>500 || req.Back.Length>500)
                return Results.BadRequest("Front/back length 1-500");
            var card = await svc.CreateAsync(userId, req.Front, req.Back, Domain.FlashcardSource.Manual);
            return Results.Ok(card);
        }).RequireAuthorization();

        app.MapPut("/flashcards/{id}", async (FlashcardService svc, ClaimsPrincipal user, string id, UpdateFlashcardRequest req) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Results.Unauthorized();
            if (string.IsNullOrWhiteSpace(req.Front) || string.IsNullOrWhiteSpace(req.Back) || req.Front.Length>500 || req.Back.Length>500)
                return Results.BadRequest("Front/back length 1-500");
            var card = await svc.UpdateAsync(userId, id, req.Front, req.Back);
            return card == null ? Results.NotFound() : Results.Ok(card);
        }).RequireAuthorization();

        app.MapDelete("/flashcards/{id}", async (FlashcardService svc, ClaimsPrincipal user, string id) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Results.Unauthorized();
            await svc.DeleteAsync(userId, id);
            return Results.NoContent();
        }).RequireAuthorization();

        app.MapPost("/flashcards/{id}/reset", async (FlashcardService svc, ClaimsPrincipal user, string id) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Results.Unauthorized();
            var card = await svc.ResetSchedulingAsync(userId, id);
            return card == null ? Results.NotFound() : Results.Ok(card);
        }).RequireAuthorization();

        app.MapPost("/flashcards/reset/all", async (FlashcardService svc, ClaimsPrincipal user) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Results.Unauthorized();
            var count = await svc.ResetAllSchedulingAsync(userId);
            return Results.Ok(new { modified = count });
        }).RequireAuthorization();

        app.MapPost("/flashcards/reset", async (FlashcardService svc, ClaimsPrincipal user, BulkResetRequest req) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Results.Unauthorized();
            if (req.Ids == null || req.Ids.Count == 0) return Results.BadRequest("Ids required");
            var count = await svc.ResetSchedulingForIdsAsync(userId, req.Ids);
            return Results.Ok(new { modified = count });
        }).RequireAuthorization();

        app.MapPost("/flashcards/generate", async (HttpContext http, GenerationService gen, ClaimsPrincipal user, GenerateFlashcardsRequest req) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Results.Unauthorized();
            var ip = http.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            if(!allow($"gen:{userId}:{ip}", 5, TimeSpan.FromMinutes(1))) return Results.StatusCode(429);
            if (req.Text.Length < 1000 || req.Text.Length > 10000) return Results.BadRequest("Text must be 1000-10000 chars");
            var result = await gen.GenerateAsync(userId, req.Text);
            return Results.Ok(new { sessionId = result.SessionId, suggestions = result.Suggestions });
        }).RequireAuthorization();

        app.MapPost("/flashcards/generate/accept", async (FlashcardService flashcards, MongoContext db, ClaimsPrincipal user, AcceptGeneratedRequest req) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Results.Unauthorized();
            var session = await db.GenerationSessions.Find(s => s.Id == req.SessionId && s.UserId == userId).FirstOrDefaultAsync();
            if (session == null) return Results.NotFound("Session not found");
            if (req.Cards == null || req.Cards.Count == 0) return Results.BadRequest("No cards supplied");
            if (req.Cards.Count > 50) return Results.BadRequest("Too many cards (max 50)");
            var created = await flashcards.BulkCreateAiAsync(userId, req.Cards.Select(c => (c.Front, c.Back)));
            var accepted = created.Count;
            var update = Builders<_10xCards.backend.Domain.GenerationSession>.Update.Set(s => s.AcceptedCount, session.AcceptedCount + accepted);
            await db.GenerationSessions.UpdateOneAsync(s => s.Id == session.Id, update);
            return Results.Ok(new { accepted, created });
        }).RequireAuthorization();
        return app;
    }
}
