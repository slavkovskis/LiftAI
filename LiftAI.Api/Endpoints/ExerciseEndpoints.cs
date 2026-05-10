using System.Security.Claims;
using LiftAI.Api.Data;
using LiftAI.Api.Data.Mapping;
using LiftAI.Shared.Models;
using LiftAI.Shared.Models.Dtos.Exercise;
using Microsoft.EntityFrameworkCore;

namespace LiftAI.Api.Endpoints;

public static class ExerciseEndpoints
{
    private static readonly ExerciseDto[] TrialExercises =
    [
        new() { Id = -1, Name = "Bench Press", MuscleGroup = "Chest", DefaultRestSeconds = 120, IsTimed = false, IsSystem = true },
        new() { Id = -2, Name = "Squat", MuscleGroup = "Legs", DefaultRestSeconds = 180, IsTimed = false, IsSystem = true },
        new() { Id = -3, Name = "Deadlift", MuscleGroup = "Back", DefaultRestSeconds = 180, IsTimed = false, IsSystem = true },
        new() { Id = -4, Name = "Overhead Press", MuscleGroup = "Shoulders", DefaultRestSeconds = 120, IsTimed = false, IsSystem = true },
        new() { Id = -5, Name = "Plank", MuscleGroup = "Core", DefaultRestSeconds = 60, IsTimed = true, IsSystem = true }
    ];

    public static void MapExerciseEndpoints(this WebApplication app)
    {
        app.MapGet("/api/exercises/trial", () => Results.Ok(TrialExercises))
            .AllowAnonymous()
            .WithTags("Exercises")
            .WithName("GetTrialExercises");

        var group = app.MapGroup("/api/exercises").RequireAuthorization();
        
        // GET /api/exercises
        group.MapGet("/", async (ApplicationDbContext db, ClaimsPrincipal user) =>
            {
                var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);

                if (string.IsNullOrEmpty(userId))
                {
                    return Results.Unauthorized();
                }
                
                var exercises = await db.Exercises
                    .Where(e => e.UserId == userId)
                    .OrderBy(e => e.Name)
                    .Select(e => e.ToDto())
                    .ToListAsync();

                return Results.Ok(exercises);
            })
            .WithTags("Exercises")
            .WithName("GetUserExercises");
        
        // GET /api/exercises/{id}
        group.MapGet("/{id:int}", async (ApplicationDbContext db, ClaimsPrincipal user, int id) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            
            if (string.IsNullOrEmpty(userId))
            {
                return Results.Unauthorized();
            }

            var exercise = await db.Exercises
                .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);
            
            return exercise is null ? Results.NotFound() : Results.Ok(exercise.ToDetailDto());
        });
        
        // POST /api/exercises
        group.MapPost("/", async (ApplicationDbContext db, ClaimsPrincipal user, ExerciseCreateDto dto) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        
            if (string.IsNullOrEmpty(userId))
            {
                return Results.Unauthorized();
            }
            
            if (string.IsNullOrWhiteSpace(dto.Name)
                || string.IsNullOrWhiteSpace(dto.MuscleGroup)
                || dto.DefaultRestSeconds < 0)
            {
                return Results.BadRequest("Invalid exercise data.");
            }

            var allowedMuscleGroups = await GetAllowedMuscleGroupsAsync(db, userId);

            if (!allowedMuscleGroups.Contains(dto.MuscleGroup))
            {
                return Results.BadRequest("Muscle group must be selected from an existing group.");
            }

            var exercise = new Exercise
            {
                UserId = userId,
                Name = dto.Name,
                MuscleGroup = dto.MuscleGroup,
                Description = dto.Description,
                IsTimed = dto.IsTimed,
                DefaultRestSeconds = dto.DefaultRestSeconds,
                IsSystem = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            db.Exercises.Add(exercise);
            await db.SaveChangesAsync();
            
            return Results.Created($"api/exercises/{exercise.Id}", exercise.ToDetailDto());
        })
        .WithTags("Exercises")
        .WithName("CreateExercise");
        
        // PUT /api/exercises/{id}
        group.MapPut("/{id:int}", async (ApplicationDbContext db, ClaimsPrincipal user, ExerciseUpdateDto dto, int id) =>
            {
                var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);

                if (string.IsNullOrEmpty(userId))
                {
                    return Results.Unauthorized();
                }

                var exercise = await db.Exercises
                    .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);

                if (exercise is null)
                {
                    return Results.NotFound();
                }

                if (exercise.IsSystem 
                    && (dto.Name != exercise.Name || dto.MuscleGroup != exercise.MuscleGroup || dto.IsTimed != exercise.IsTimed))
                {
                    return Results.BadRequest("For system exercises, only their description and rest time can be edited.");
                }

                if (string.IsNullOrWhiteSpace(dto.Name)
                    || string.IsNullOrWhiteSpace(dto.MuscleGroup)
                    || dto.DefaultRestSeconds < 0)
                {
                    return Results.BadRequest("Invalid exercise data.");
                }

                var allowedMuscleGroups = await GetAllowedMuscleGroupsAsync(db, userId);

                if (!allowedMuscleGroups.Contains(dto.MuscleGroup))
                {
                    return Results.BadRequest("Muscle group must be selected from an existing group.");
                }

                if (!exercise.IsSystem)
                {
                    exercise.Name = dto.Name;
                    exercise.MuscleGroup = dto.MuscleGroup;
                    exercise.IsTimed = dto.IsTimed;
                    exercise.Description = dto.Description;
                    exercise.DefaultRestSeconds = dto.DefaultRestSeconds;
                }
                else
                {
                    exercise.Description = dto.Description;
                    exercise.DefaultRestSeconds = dto.DefaultRestSeconds;
                }
                
                exercise.UpdatedAt = DateTime.UtcNow;

                await db.SaveChangesAsync();

                return Results.Ok(exercise.ToDto());
            })
            .WithTags("Exercises")
            .WithName("UpdateExercise");
        
        // DELETE /api/exercises/{id}
        group.MapDelete("/{id:int}", async (ApplicationDbContext db, ClaimsPrincipal user, int id) =>
            {
                var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);

                if (string.IsNullOrEmpty(userId))
                {
                    return Results.Unauthorized();
                }

                var exercise = await db.Exercises
                    .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);

                if (exercise is null)
                {
                    return Results.NotFound();
                }

                if (exercise.IsSystem)
                {
                    return Results.BadRequest("System exercises cannot be deleted.");
                }
                    
                db.Exercises.Remove(exercise);
                await db.SaveChangesAsync();

                return Results.NoContent();
            })
            .WithTags("Exercises")
            .WithName("DeleteExercise");
    }

    private static async Task<HashSet<string>> GetAllowedMuscleGroupsAsync(ApplicationDbContext db, string userId)
    {
        var groups = await db.Exercises
            .Where(e => e.UserId == userId)
            .Select(e => e.MuscleGroup)
            .Where(group => !string.IsNullOrWhiteSpace(group))
            .Distinct()
            .ToListAsync();

        return new HashSet<string>(groups, StringComparer.OrdinalIgnoreCase);
    }
}