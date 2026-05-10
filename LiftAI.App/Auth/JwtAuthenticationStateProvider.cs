using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using System.Security.Claims;
using System.Text.Json;

namespace LiftAI.App.Auth;

public class JwtAuthenticationStateProvider(IJSRuntime jsRuntime, ILogger<JwtAuthenticationStateProvider> logger)
    : AuthenticationStateProvider
{
    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            var token = await jsRuntime.InvokeAsync<string?>("localStorage.getItem", "authToken");

            if (string.IsNullOrWhiteSpace(token))
                return AnonymousState();

            // Check if token is expired
            if (IsTokenExpired(token))
            {
                await jsRuntime.InvokeVoidAsync("localStorage.removeItem", "authToken");
                return AnonymousState();
            }

            var claims = ParseClaimsFromJwt(token);
            var identity = new ClaimsIdentity(claims, "jwt");
            var user = new ClaimsPrincipal(identity);

            return new AuthenticationState(user);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to get authentication state from localStorage");
            return AnonymousState();
        }
    }

    public async Task MarkUserAsAuthenticated(string token)
    {
        await jsRuntime.InvokeVoidAsync("localStorage.setItem", "authToken", token);
        var claims = ParseClaimsFromJwt(token);
        var identity = new ClaimsIdentity(claims, "jwt");

        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(new ClaimsPrincipal(identity))));
    }

    public async Task MarkUserAsLoggedOut()
    {
        await jsRuntime.InvokeVoidAsync("localStorage.removeItem", "authToken");
        NotifyAuthenticationStateChanged(Task.FromResult(AnonymousState()));
    }

    private static AuthenticationState AnonymousState() 
        => new(new ClaimsPrincipal(new ClaimsIdentity()));

    private static bool IsTokenExpired(string token)
    {
        try
        {
            var claims = ParseClaimsFromJwt(token);
            var expClaim = claims.FirstOrDefault(c => c.Type == "exp")?.Value;

            if (string.IsNullOrEmpty(expClaim) || !long.TryParse(expClaim, out var expUnix))
                return false;

            var expirationDate = DateTimeOffset.FromUnixTimeSeconds(expUnix);
            return expirationDate <= DateTimeOffset.UtcNow;
        }
        catch
        {
            return true;
        }
    }

    private static List<Claim> ParseClaimsFromJwt(string jwt)
    {
        var claims = new List<Claim>();
        var payload = jwt.Split('.')[1];
        var jsonBytes = DecodeBase64Url(payload);

        using var document = JsonDocument.Parse(jsonBytes);

        foreach (var property in document.RootElement.EnumerateObject())
        {
            var value = property.Value.ToString();
            claims.Add(new Claim(MapClaimType(property.Name), value));
        }

        return claims;
    }

    private static byte[] DecodeBase64Url(string input)
    {
        var base64 = input.Replace('-', '+').Replace('_', '/');
        base64 = base64.PadRight(base64.Length + (4 - base64.Length % 4) % 4, '=');
        return Convert.FromBase64String(base64);
    }

    private static string MapClaimType(string jwtClaim) => jwtClaim switch
    {
        "sub" => ClaimTypes.NameIdentifier,
        "name" => ClaimTypes.Name,
        "email" => ClaimTypes.Email,
        "role" => ClaimTypes.Role,
        _ => jwtClaim
    };
}