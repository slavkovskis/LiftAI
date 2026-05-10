namespace LiftAI.Shared.Models.Dtos.Chat;

public class ChatResponseDto
{
    public bool Success { get; set; }
    public int ConversationId { get; set; }
    public required string Response { get; set; }
    public bool IsQuotaLimited { get; set; }
    public int? RemainingRequestsToday { get; set; }
    public required string Message { get; set; }
}