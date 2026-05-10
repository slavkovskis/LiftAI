using LiftAI.Shared.Models.Dtos.Chat;
using LiftAI.Shared.Models.Enums;
using Microsoft.Extensions.Options;

namespace LiftAI.Api.Services.Ollama;

public class OllamaChatClient(
    HttpClient httpClient,
    IOptions<OllamaOptions> options,
    ILogger<OllamaChatClient> logger) : IOllamaChatClient
{
    private readonly OllamaOptions _options = options.Value;

    public async Task<string?> SendAsync(IReadOnlyList<ChatMessageDto> messages, CancellationToken ct = default)
    {
        if (messages.Count == 0)
        {
            return null;
        }

        var request = new
        {
            model = _options.Model,
            stream = false,
            messages = messages.Select(m => new
            {
                role = MapRole(m.Role),
                content = m.Content
            }).ToList()
        };
        
        try
        {
            var endpoint = $"{_options.BaseUrl.TrimEnd('/')}/api/chat";
            var response = await httpClient.PostAsJsonAsync(endpoint, request, ct);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Ollama chat failed. Status: {StatusCode}", response.StatusCode);
                return null;
            }

            var payload = await response.Content.ReadFromJsonAsync<OllamaChatResponse>(ct);

            return payload?.Message?.Content;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ollama chat request failed");
            return null;
        }
    }
    
    private static string MapRole(ChatRole role) => role switch
    {
        ChatRole.System => "system",
        ChatRole.User => "user",
        ChatRole.Assistant => "assistant",
        _ => "user"
    };
    
    private sealed class OllamaChatResponse
    {
        public OllamaMessage? Message { get; set; }
    }

    private sealed class OllamaMessage
    {
        public string? Content { get; set; }
    }
}