namespace LiftAI.Shared.Models.Dtos.Exercise;

public class ExerciseDetailDto
{
    public required string Name { get; set; }
    public required string MuscleGroup { get; set; }
    public string? Description { get; set; }
    public int DefaultRestSeconds { get; set; }
    public bool IsTimed { get; set; }
    public bool IsSystem { get; set; }
}