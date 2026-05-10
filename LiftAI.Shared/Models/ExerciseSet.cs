namespace LiftAI.Shared.Models;

public class ExerciseSet
{
    public int Id { get; set; }

    public int WorkoutExerciseId { get; set; }
    public WorkoutExercise WorkoutExercise { get; set; } = null!;

    public int SetNumber { get; set; }
    
    public int? Reps { get; set; }
    public int? DurationSeconds { get; set; }
    public decimal Weight { get; set; }          // in kg
    public int? RIR { get; set; }                // Reps In Reserve (very useful for AI)
    public string? Notes { get; set; }
}