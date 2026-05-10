namespace LiftAI.Shared.Models.Dtos.Conversation;

public class ChatConversationDto
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public DateTime LastMessageAt { get; set; }
    public DateTime CreatedAt { get; set; }
}