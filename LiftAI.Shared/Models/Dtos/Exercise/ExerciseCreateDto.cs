namespace LiftAI.Shared.Models.Dtos.Exercise;

public class ExerciseCreateDto
{
    public required string Name { get; set; }
    public required string MuscleGroup { get; set; }
    public string? Description { get; set; }
    public int DefaultRestSeconds { get; set; } = 120;
    public bool IsTimed { get; set; } = false;
}