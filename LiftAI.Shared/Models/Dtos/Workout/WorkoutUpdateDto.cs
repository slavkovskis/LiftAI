namespace LiftAI.Shared.Models.Dtos.Workout;

public class WorkoutUpdateDto
{
    // Nullable so the client can patch only what it wants to change for now.
    public DateTime? Date { get; set; }
    public string? Notes { get; set; }
    public int? DurationMinutes { get; set; }
}