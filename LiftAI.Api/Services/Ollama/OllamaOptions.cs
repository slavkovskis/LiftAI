namespace LiftAI.Api.Services.Ollama;

public sealed class OllamaOptions
{
    public string BaseUrl { get; set; } = "http://localhost:11434";
    public string Model { get; set; } = "llama3.1:8b";
    public int TimeoutSeconds { get; set; } = 120;
}