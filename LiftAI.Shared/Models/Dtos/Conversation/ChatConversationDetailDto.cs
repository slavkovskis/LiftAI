using LiftAI.Shared.Models.Dtos.Chat;

namespace LiftAI.Shared.Models.Dtos.Conversation;

public class ChatConversationDetailDto
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public required List<ChatMessageDto> Messages { get; set; }
    public DateTime LastMessageAt { get; set; }
}