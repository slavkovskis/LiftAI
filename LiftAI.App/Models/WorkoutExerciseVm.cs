namespace LiftAI.App.Models;

public class WorkoutExerciseVm
{
    public int ExerciseId { get; set; }

    public string ExerciseName { get; set; } = string.Empty;
    public string MuscleGroup { get; set; } = string.Empty;
    public string? Description { get; set; }

    public int DefaultRestSeconds { get; set; }
    public bool IsTimed { get; set; }

    public string? Notes { get; set; }

    public List<ExerciseSetVm> Sets { get; set; } = new();
}