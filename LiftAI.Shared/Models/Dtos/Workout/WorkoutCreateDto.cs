namespace LiftAI.Shared.Models.Dtos.Workout;

public class WorkoutCreateDto
{
    public DateTime Date { get; set; } = DateTime.UtcNow.Date;
    public string? Notes { get; set; }
    public int? DurationMinutes { get; set; }

    // The actual exercises and sets the user performed
    public List<WorkoutExerciseCreateDto> Exercises { get; set; } = new();
}

public class WorkoutExerciseCreateDto
{
    public int ExerciseId { get; set; }
    public string? Notes { get; set; }
    public string? ExerciseDescription { get; set; }
    public List<ExerciseSetCreateDto> Sets { get; set; } = new();
}

public class ExerciseSetCreateDto
{
    public int SetNumber { get; set; }
    public int? Reps { get; set; }  
    public int? DurationSeconds { get; set; }
    public decimal Weight { get; set; } = 0m; // If user does not add a weight so there is no bugs
    public int? RIR { get; set; }
    public string? Notes { get; set; }
}