using System.Security.Claims;
using LiftAI.Api.Data;
using LiftAI.Api.Data.Mapping;
using LiftAI.Shared.Models;
using LiftAI.Shared.Models.Dtos.Workout;
using Microsoft.EntityFrameworkCore;

namespace LiftAI.Api.Endpoints;

public static class WorkoutEndpoints
{
    private static readonly HashSet<int> TrialExerciseIds = [-1, -2, -3, -4, -5];

    private static bool HasInvalidSets(WorkoutCreateDto dto) =>
        dto.Exercises.Any(exercise => exercise.Sets.Any(set => !IsValidSet(set)));

    private static bool IsValidSet(ExerciseSetCreateDto set) =>
        set.Reps.GetValueOrDefault() > 0 || set.Weight > 0m || set.DurationSeconds.GetValueOrDefault() > 0;

    public static void MapWorkoutEndpoints(this WebApplication app)
    {
        app.MapPost("/api/workouts/guest-complete", (WorkoutCreateDto dto) =>
            {
                if (dto.Exercises.Count == 0)
                {
                    return Results.BadRequest("A workout must contain at least one exercise.");
                }

                var invalidExerciseIds = dto.Exercises
                    .Select(exercise => exercise.ExerciseId)
                    .Where(exerciseId => !TrialExerciseIds.Contains(exerciseId))
                    .Distinct()
                    .ToList();

                if (invalidExerciseIds.Count > 0)
                {
                    return Results.BadRequest(new
                    {
                        Message = $"Only trial exercises are allowed for guest workouts. Invalid id(s): {string.Join(", ", invalidExerciseIds)}"
                    });
                }

                if (HasInvalidSets(dto))
                {
                    return Results.BadRequest("You cannot save a workout with incomplete sets.");
                }

                return Results.Ok(new
                {
                    Saved = false,
                    Message = "Guest workout completed. Create an account to save workouts permanently."
                });
            })
            .AllowAnonymous()
            .WithTags("Workouts")
            .WithName("CompleteGuestWorkout");

        var group = app.MapGroup("/api/workouts").RequireAuthorization();
        
        // GET /api/workouts
        group.MapGet("/", async (ApplicationDbContext db, ClaimsPrincipal user) =>
            {
                var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
                
                if (string.IsNullOrEmpty(userId))
                {
                    return Results.Unauthorized();
                }
                
                var workouts = await db.Workouts
                    .Where(w => w.UserId == userId)
                    .OrderByDescending(w => w.Date)
                    .Select(w => w.ToDto())
                    .ToListAsync();

                return Results.Ok(workouts);
            })
            .WithTags("Workouts")
            .WithName("GetUserWorkouts");
        
        // GET /api/workouts/{id}
        group.MapGet("/{id:int}", async (ApplicationDbContext db, ClaimsPrincipal user, int id) =>
            {
                var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
                
                if (string.IsNullOrEmpty(userId))
                {
                    return Results.Unauthorized();
                }
                
                var workout = await db.Workouts
                    .AsNoTracking()
                    .Include(w => w.Exercises)
                    .ThenInclude(we => we.Exercise)
                    .Include(w => w.Exercises)
                    .ThenInclude(we => we.Sets)
                    .AsSplitQuery()
                    .FirstOrDefaultAsync(w => w.Id == id && w.UserId == userId);

                return workout is null ? Results.NotFound() : Results.Ok(workout.ToDetailDto());
            })
            .WithTags("Workouts")
            .WithName("GetWorkoutById");
        
        // POST /api/workouts
        group.MapPost("/", async (ApplicationDbContext db, ClaimsPrincipal user, WorkoutCreateDto dto) =>
            {
                var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
                
                if (string.IsNullOrEmpty(userId))
                {
                    return Results.Unauthorized();
                }

                var exerciseIds = dto.Exercises
                    .Select(x => x.ExerciseId)
                    .Distinct()
                    .ToList();
                
                if (exerciseIds.Count == 0)
                {
                    return Results.BadRequest("A workout must contain at least one exercise.");
                }

                if (HasInvalidSets(dto))
                {
                    return Results.BadRequest("You cannot save a workout with incomplete sets.");
                }
                
                var exercisesById = await db.Exercises
                    .Where(e => exerciseIds.Contains(e.Id) && e.UserId == userId)
                    .ToDictionaryAsync(e => e.Id);
                
                var missingExerciseIds = exerciseIds
                    .Where(id => !exercisesById.ContainsKey(id))
                    .ToList();
                
                if (missingExerciseIds.Count > 0)
                {
                    return Results.BadRequest(new
                    {
                        Message = $"Unknown exercise id(s): {string.Join(", ", missingExerciseIds)}"
                    });
                }

                foreach (var exerciseDto in dto.Exercises)
                {
                    if (string.IsNullOrWhiteSpace(exerciseDto.ExerciseDescription))
                    {
                        continue;
                    }

                    var exercise = exercisesById[exerciseDto.ExerciseId];
                    exercise.Description = exerciseDto.ExerciseDescription.Trim();
                    exercise.UpdatedAt = DateTime.UtcNow;
                }

                var workout = new Workout
                {
                    UserId = userId,
                    Date = dto.Date,
                    Notes = dto.Notes,
                    DurationMinutes = dto.DurationMinutes,
                    Exercises = dto.Exercises.Select((exerciseDto, index) => new WorkoutExercise
                    {
                        ExerciseId = exerciseDto.ExerciseId,
                        Exercise = exercisesById[exerciseDto.ExerciseId],
                        Order = index + 1,
                        Notes = exerciseDto.Notes,
                        Sets = exerciseDto.Sets.Select(setDto => new ExerciseSet
                        {
                            SetNumber = setDto.SetNumber,
                            Reps = setDto.Reps,
                            DurationSeconds = setDto.DurationSeconds,
                            Weight = setDto.Weight,
                            RIR = setDto.RIR,
                            Notes = setDto.Notes
                        }).ToList()
                    }).ToList()
                };
                
                db.Workouts.Add(workout);
                await db.SaveChangesAsync();

                return Results.Created($"/api/workouts/{workout.Id}", workout.ToDetailDto());
            })
            .WithTags("Workouts")
            .WithName("CreateWorkout");
        
        // PUT /api/workouts/{id}
        group.MapPut("/{id:int}", async (ApplicationDbContext db, ClaimsPrincipal user, WorkoutCreateDto dto, int id) =>
            {
                var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
                
                if (string.IsNullOrEmpty(userId))
                {
                    return Results.Unauthorized();
                }
                
                if (dto.Exercises.Count == 0)
                {
                    return Results.BadRequest("A workout must contain at least one exercise.");
                }

                if (dto.DurationMinutes.HasValue && dto.DurationMinutes.Value < 0)
                {
                    return Results.BadRequest("Duration must be zero or a positive number.");
                }

                if (HasInvalidSets(dto))
                {
                    return Results.BadRequest("You cannot save a workout with incomplete sets.");
                }

                var workout = await db.Workouts
                    .Include(w => w.Exercises)
                    .ThenInclude(we => we.Sets)
                    .FirstOrDefaultAsync(w => w.Id == id && w.UserId == userId);

                if (workout is null)
                {
                    return Results.NotFound();
                }

                var exerciseIds = dto.Exercises
                    .Select(x => x.ExerciseId)
                    .Distinct()
                    .ToList();

                var exercisesById = await db.Exercises
                    .Where(e => exerciseIds.Contains(e.Id) && e.UserId == userId)
                    .ToDictionaryAsync(e => e.Id);

                var missingExerciseIds = exerciseIds
                    .Where(exerciseId => !exercisesById.ContainsKey(exerciseId))
                    .ToList();

                if (missingExerciseIds.Count > 0)
                {
                    return Results.BadRequest(new
                    {
                        Message = $"Unknown exercise id(s): {string.Join(", ", missingExerciseIds)}"
                    });
                }

                await using var transaction = await db.Database.BeginTransactionAsync();

                workout.Date = dto.Date;
                workout.Notes = dto.Notes;
                workout.DurationMinutes = dto.DurationMinutes;
                workout.UpdatedAt = DateTime.UtcNow;

                foreach (var exerciseDto in dto.Exercises)
                {
                    if (string.IsNullOrWhiteSpace(exerciseDto.ExerciseDescription))
                    {
                        continue;
                    }

                    var exercise = exercisesById[exerciseDto.ExerciseId];
                    exercise.Description = exerciseDto.ExerciseDescription.Trim();
                    exercise.UpdatedAt = DateTime.UtcNow;
                }

                workout.Exercises.Clear();

                foreach (var (exerciseDto, index) in dto.Exercises.Select((exerciseDto, index) => (exerciseDto, index)))
                {
                    workout.Exercises.Add(new WorkoutExercise
                    {
                        ExerciseId = exerciseDto.ExerciseId,
                        Exercise = exercisesById[exerciseDto.ExerciseId],
                        Order = index + 1,
                        Notes = exerciseDto.Notes,
                        Sets = exerciseDto.Sets.Select(setDto => new ExerciseSet
                        {
                            SetNumber = setDto.SetNumber,
                            Reps = setDto.Reps,
                            DurationSeconds = setDto.DurationSeconds,
                            Weight = setDto.Weight,
                            RIR = setDto.RIR,
                            Notes = setDto.Notes
                        }).ToList()
                    });
                }

                await db.SaveChangesAsync();
                await transaction.CommitAsync();

                return Results.Ok(workout.ToDetailDto());
            })
            .WithTags("Workouts")
            .WithName("UpdateWorkout");
        
        // DELETE /api/workouts/{id}
        group.MapDelete("/{id:int}", async (ApplicationDbContext db, ClaimsPrincipal user, int id) =>
            {
                var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
                
                if (string.IsNullOrEmpty(userId))
                {
                    return Results.Unauthorized();
                }
                
                var workout = await db.Workouts.FirstOrDefaultAsync(w => w.Id == id && w.UserId == userId);

                if (workout is null)
                {
                    return Results.NotFound();
                }

                db.Workouts.Remove(workout);
                await db.SaveChangesAsync();

                return Results.NoContent();
            })
            .WithTags("Workouts")
            .WithName("DeleteWorkout");
    }
}