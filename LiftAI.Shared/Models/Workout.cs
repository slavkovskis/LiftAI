namespace LiftAI.Shared.Models;

public class Workout
{
    public int Id { get; set; }
    
    public string UserId { get; set; } = string.Empty;

    public DateTime Date { get; set; } = DateTime.UtcNow.Date;
    public string? Notes { get; set; }
    public int? DurationMinutes { get; set; } 

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<WorkoutExercise> Exercises { get; set; } = new List<WorkoutExercise>();
}