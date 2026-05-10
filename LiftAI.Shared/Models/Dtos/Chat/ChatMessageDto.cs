using LiftAI.Shared.Models.Enums;

namespace LiftAI.Shared.Models.Dtos.Chat;

public class ChatMessageDto
{
    public ChatRole Role { get; set; }
    public required string Content { get; set; }
}