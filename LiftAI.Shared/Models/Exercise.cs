using System.ComponentModel.DataAnnotations;

namespace LiftAI.Shared.Models;

public class Exercise
{
    public int Id { get; set; }

    public required string UserId { get; set; }

    public required string Name { get; set; }
    public required string MuscleGroup { get; set; }
    public string? Description { get; set; }
    public bool IsTimed { get; set; }

    [Range(0, int.MaxValue)]
    public int DefaultRestSeconds { get; set; } = 120;   

    public bool IsSystem { get; set; }          

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}