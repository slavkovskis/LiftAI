using LiftAI.Shared.Models.Enums;

namespace LiftAI.Api.Data.Models;

public class ChatMessageEntity
{
    public int Id { get; set; }
    public int ConversationId { get; set; }
    public ChatConversation Conversation { get; set; } = null!;
    public ChatRole Role { get; set; }
    public required string Content { get; set; }
    public DateTime CreatedAt { get; set; }
    public int? PromptTokens { get; set; }
    public int? CompletionTokens { get; set; }
}