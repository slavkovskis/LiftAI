namespace LiftAI.Api.Services.Email;

public sealed class SmtpOptions
{
    public string Host { get; init; } = "";
    public int Port { get; init; }
    public string User { get; init; } = "";
    public string Password { get; init; } = "";
    public string From { get; init; } = "";
    public bool EnableSsl { get; init; } = true;
}