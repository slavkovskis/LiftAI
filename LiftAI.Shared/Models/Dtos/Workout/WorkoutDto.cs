namespace LiftAI.Shared.Models.Dtos.Workout;

public class WorkoutDto
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public string? Notes { get; set; }
    public int? DurationMinutes { get; set; }
}