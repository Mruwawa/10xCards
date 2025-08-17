using System.Security.Claims;
using _10xCards.backend.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace _10xCards.backend.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuth(this IEndpointRouteBuilder app, Func<string, int, TimeSpan, bool> allow)
    {
        app.MapPost("/auth/register", async (AuthService auth, RegisterRequest req) =>
        {
            var user = await auth.RegisterAsync(req.Email, req.Password);
            if (user == null) return Results.Conflict("Email in use");
            var token = auth.GenerateJwt(user);
            return Results.Ok(new { token });
        });

        app.MapPost("/auth/login", async (HttpContext http, AuthService auth, LoginRequest req) =>
        {
            var ip = http.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            if(!allow($"login:{ip}", 10, TimeSpan.FromMinutes(1))) return Results.StatusCode(429);
            var user = await auth.ValidateUserAsync(req.Email, req.Password);
            if (user == null) return Results.Unauthorized();
            var token = auth.GenerateJwt(user);
            return Results.Ok(new { token });
        });
        return app;
    }
}
