namespace LiftAI.Shared.Models;

public class WorkoutExercise
{
    public int Id { get; set; }

    public int WorkoutId { get; set; }
    public Workout Workout { get; set; } = null!;

    public int ExerciseId { get; set; }
    public Exercise Exercise { get; set; } = null!;

    public int Order { get; set; }                        // order inside the workout
    public string? Notes { get; set; }
    
    public ICollection<ExerciseSet> Sets { get; set; } = new List<ExerciseSet>();
}