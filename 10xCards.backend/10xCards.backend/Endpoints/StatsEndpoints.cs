using System.Security.Claims;
using _10xCards.backend.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace _10xCards.backend.Endpoints;

public static class StatsEndpoints
{
    public static IEndpointRouteBuilder MapStats(this IEndpointRouteBuilder app)
    {
        app.MapGet("/stats/generation", async (StatsService stats, ClaimsPrincipal user) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Results.Unauthorized();
            var data = await stats.GetGenerationStatsAsync(userId);
            return Results.Ok(data);
        }).RequireAuthorization();

        app.MapGet("/stats/study/today", async (StatsService stats, ClaimsPrincipal user) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Results.Unauthorized();
            var data = await stats.GetTodayStudyStatsAsync(userId);
            return Results.Ok(data);
        }).RequireAuthorization();
        return app;
    }
}
