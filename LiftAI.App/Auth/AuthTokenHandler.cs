using System.Net.Http.Headers;
using Microsoft.JSInterop;

namespace LiftAI.App.Auth;

public class AuthTokenHandler(IJSRuntime jsRuntime, ILogger<AuthTokenHandler> logger)
    : DelegatingHandler
{
    private const string TokenKey = "authToken";

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        try
        {
            var token = await jsRuntime.InvokeAsync<string?>(
                "localStorage.getItem",
                cancellationToken,
                TokenKey);

            if (!string.IsNullOrWhiteSpace(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Failed to read JWT token from localStorage.");
        }
        
        return await base.SendAsync(request, cancellationToken);
    }
}