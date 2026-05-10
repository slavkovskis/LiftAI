using LiftAI.Shared.Models.Dtos.Chat;

namespace LiftAI.Api.Services.Ollama;

public interface IOllamaChatClient
{
    Task<string?> SendAsync(IReadOnlyList<ChatMessageDto> messages, CancellationToken ct = default);
}