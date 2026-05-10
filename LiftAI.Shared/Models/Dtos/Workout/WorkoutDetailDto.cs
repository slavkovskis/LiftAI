namespace LiftAI.Shared.Models.Dtos.Workout;

public class WorkoutDetailDto
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public string? Notes { get; set; }
    public int? DurationMinutes { get; set; }

    // Full details - exercises performed in this workout
    public List<WorkoutExerciseDetailDto> Exercises { get; set; } = new();
}

public class WorkoutExerciseDetailDto
{
    public int Id { get; set; }
    public int ExerciseId { get; set; }
    public string ExerciseName { get; set; } = string.Empty;
    public string ExerciseDescription { get; set; } = string.Empty;
    public string MuscleGroup { get; set; } = string.Empty;
    public int DefaultRestSeconds { get; set; }
    public bool IsTimed { get; set; }
    public string? Notes { get; set; }
    public int Order { get; set; }

    public List<ExerciseSetDetailDto> Sets { get; set; } = new();
}

public class ExerciseSetDetailDto
{
    public int Id { get; set; }
    public int SetNumber { get; set; }

    public int? Reps { get; set; }
    public int? DurationSeconds { get; set; }
    public decimal Weight { get; set; } = 0m; // If user does not add a weight so there is no bugs
    public int? RIR { get; set; }
    public string? Notes { get; set; }
}