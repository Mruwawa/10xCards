using System.Security.Claims;
using _10xCards.backend.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using MongoDB.Driver;

namespace _10xCards.backend.Endpoints;

public static class StudyEndpoints
{
    public static IEndpointRouteBuilder MapStudy(this IEndpointRouteBuilder app)
    {
        app.MapGet("/study/next", async (ReviewService review, ClaimsPrincipal user, string? exclude) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Results.Unauthorized();
            var card = await review.GetNextDueAsync(userId, null, exclude);
            if (card == null) return Results.NoContent();
            return Results.Ok(new { card.Id, card.Front });
        }).RequireAuthorization();

        app.MapPost("/study/review", async (ReviewService review, ClaimsPrincipal user, StudyReviewRequest req) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Results.Unauthorized();
            var updated = await review.ReviewAsync(userId, req.FlashcardId, req.Quality);
            if (updated == null) return Results.NotFound();
            return Results.Ok(new { nextReview = updated.NextReview, intervalDays = updated.IntervalDays, easeFactor = updated.EaseFactor });
        }).RequireAuthorization();
        return app;
    }
}
