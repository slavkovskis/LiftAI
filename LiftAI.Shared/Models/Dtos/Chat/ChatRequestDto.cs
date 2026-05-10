namespace LiftAI.Shared.Models.Dtos.Chat;

public enum ContextMode
{
    Normal = 0,
    Deep = 1
}

public class ChatRequestDto
{
    public int? ConversationId { get; set; }
    public required List<ChatMessageDto> Messages { get; set; }
    public ContextMode ContextMode { get; set; } = ContextMode.Normal;
}