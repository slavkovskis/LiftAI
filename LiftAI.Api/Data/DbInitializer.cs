using LiftAI.Shared.Models;

namespace LiftAI.Api.Data;

public static class DbInitializer
{
    public static void Initialize(ApplicationDbContext context)
    {
        var users = context.Users.ToList();
        
        if (users.Count == 0)
            return;

        var addedAny = false;

        foreach (var user in users)
        {
            addedAny |= SeedDefaultExercisesForUser(context, user.Id);
        }

        if (addedAny)
        {
            context.SaveChanges();
        }
    }

    public static bool SeedDefaultExercisesForUser(ApplicationDbContext context, string userId)
    {
        var defaultExerciseTemplates = GetDefaultExerciseTemplates();
        var exercisesToAdd = new List<Exercise>();

        foreach (var (name, muscleGroup, restSeconds, isTimed) in defaultExerciseTemplates)
        {
            var exists = context.Exercises.Any(e =>
                e.UserId == userId &&
                e.IsSystem &&
                e.Name == name);

            if (!exists)
            {
                exercisesToAdd.Add(new Exercise
                {
                    Name = name,
                    MuscleGroup = muscleGroup,
                    UserId = userId,
                    IsSystem = true,
                    DefaultRestSeconds = restSeconds,
                    IsTimed = isTimed
                });
            }
        }

        if (exercisesToAdd.Count == 0)
        {
            return false;
        }

        context.Exercises.AddRange(exercisesToAdd);
        return true;
    }

    private static List<(string Name, string MuscleGroup, int DefaultRestSeconds, bool IsTimed)> GetDefaultExerciseTemplates()
    {
        return
        [
            ("Bench Press", "Chest", 120, false),
            ("Squat", "Legs", 180, false),
            ("Deadlift", "Back", 180, false),
            ("Overhead Press", "Shoulders", 120, false),
            ("Pull-up", "Back", 90, false),
            ("Barbell Row", "Back", 90, false),
            ("Plank", "Core", 60, true),
            ("Bicep Curl", "Arms", 60, false)
        ];
    }
}