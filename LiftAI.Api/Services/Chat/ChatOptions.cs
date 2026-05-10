namespace LiftAI.Api.Services.Chat;

public sealed class ChatOptions
{
    public int MaxClientMessages { get; set; } = 30;
    public int MaxPersistedMessagesForPrompt { get; set; } = 60;
    public int NormalWorkoutLimit { get; set; } = 7;
    public int DeepWorkoutLimit { get; set; } = 30;
}