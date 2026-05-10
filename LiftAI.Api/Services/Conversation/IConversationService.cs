using LiftAI.Shared.Models.Dtos.Chat;
using LiftAI.Shared.Models.Dtos.Conversation;
using LiftAI.Shared.Models.Enums;

namespace LiftAI.Api.Services.Conversation;

public interface IConversationService
{
    Task<int> EnsureConversationAsync(string userId, int? conversationId, CancellationToken ct = default);
    Task SaveMessageAsync(int conversationId, ChatRole role, string content, CancellationToken ct = default);
    Task<List<ChatConversationDto>> GetConversationsAsync(string userId, CancellationToken ct = default);
    Task<ChatConversationDetailDto?> GetConversationAsync(string userId, int conversationId, CancellationToken ct = default);
    Task<bool> DeleteConversationAsync(string userId, int conversationId, CancellationToken ct = default);
}