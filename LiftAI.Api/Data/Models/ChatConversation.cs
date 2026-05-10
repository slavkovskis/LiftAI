namespace LiftAI.Api.Data.Models;

public class ChatConversation
{
    public int Id { get; set; }
    public required string UserId { get; set; }
    public required string Title { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime LastMessageAt { get; set; }
    public ICollection<ChatMessageEntity> Messages { get; set; } = new List<ChatMessageEntity>();
}