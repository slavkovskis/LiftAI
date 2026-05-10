namespace LiftAI.App.Models;

public class ExerciseSetVm
{
    public int SetNumber { get; set; }
    public bool IsCompleted { get; set; }

    public int? Reps { get; set; }
    public int? DurationSeconds { get; set; }
    public decimal Weight { get; set; } = 0m;
    public int? RIR { get; set; }
    public string? Notes { get; set; }
}