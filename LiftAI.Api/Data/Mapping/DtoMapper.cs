using LiftAI.Shared.Models;
using LiftAI.Shared.Models.Dtos.Workout;
using LiftAI.Shared.Models.Dtos.Exercise;

namespace LiftAI.Api.Data.Mapping;

public static class DtoMapper
{
    extension(Exercise exercise)
    {
        public ExerciseDto ToDto()
        {
            return new ExerciseDto
            {
                Id = exercise.Id,
                Name = exercise.Name,
                MuscleGroup = exercise.MuscleGroup,
                Description = exercise.Description,
                DefaultRestSeconds = exercise.DefaultRestSeconds,
                IsTimed = exercise.IsTimed,
                IsSystem = exercise.IsSystem
            };
        }

        public ExerciseDetailDto ToDetailDto()
        {
            return new ExerciseDetailDto
            {
                Name = exercise.Name,
                MuscleGroup = exercise.MuscleGroup,
                Description = exercise.Description,
                DefaultRestSeconds = exercise.DefaultRestSeconds,
                IsTimed = exercise.IsTimed,
                IsSystem = exercise.IsSystem
            };
        }
    }

    extension(Workout workout)
    {
        public WorkoutDto ToDto()
        {
            return new WorkoutDto
            {
                Id = workout.Id,
                Date = workout.Date,
                Notes = workout.Notes,
                DurationMinutes = workout.DurationMinutes
            };
        }

        public WorkoutDetailDto ToDetailDto()
        {
            return new WorkoutDetailDto
            {
                Id = workout.Id,
                Date = workout.Date,
                Notes = workout.Notes,
                DurationMinutes = workout.DurationMinutes,
                Exercises = workout.Exercises
                    .OrderBy(we => we.Order)
                    .Select(we => new WorkoutExerciseDetailDto
                    {
                        Id = we.Id,
                        ExerciseId = we.ExerciseId,
                        ExerciseName = we.Exercise.Name,
                        ExerciseDescription = we.Exercise.Description ?? string.Empty,
                        MuscleGroup = we.Exercise.MuscleGroup,
                        DefaultRestSeconds = we.Exercise.DefaultRestSeconds,
                        IsTimed = we.Exercise.IsTimed,
                        Notes = we.Notes,
                        Order = we.Order,
                        Sets = we.Sets
                            .OrderBy(s => s.SetNumber)
                            .Select(s => new ExerciseSetDetailDto
                            {
                                Id = s.Id,
                                SetNumber = s.SetNumber,
                                Reps = s.Reps,
                                DurationSeconds = s.DurationSeconds,
                                Weight = s.Weight,
                                RIR = s.RIR,
                                Notes = s.Notes
                            })
                            .ToList()
                    })
                    .ToList()
            };
        }
    }
}