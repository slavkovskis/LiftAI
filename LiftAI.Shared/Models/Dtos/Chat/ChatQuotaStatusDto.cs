namespace LiftAI.Shared.Models.Dtos.Chat;

public class ChatQuotaStatusDto
{
    public bool IsPremium { get; set; }
    public bool IsQuotaLimited { get; set; }
    public int? RemainingRequestsToday { get; set; }
}
