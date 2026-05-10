using System.ComponentModel.DataAnnotations;

namespace LiftAI.Shared.Models.Dtos.Exercise;

public class ExerciseUpdateDto
{
    public required string Name { get; set; }
    public required string MuscleGroup { get; set; }
    public string? Description { get; set; }
    [Range(0, int.MaxValue)]
    public int DefaultRestSeconds { get; set; }
    public bool IsTimed { get; set; }
}