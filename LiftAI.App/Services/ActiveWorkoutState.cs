using LiftAI.App.Models;
using LiftAI.Shared.Models.Dtos.Exercise;
using LiftAI.Shared.Models.Dtos.Workout;

namespace LiftAI.App.Services;

public class ActiveWorkoutState
{
    private readonly List<WorkoutExerciseVm> _exercises = [];

    public event Action? OnChange;

    public DateTime StartTime { get; private set; } = DateTime.UtcNow;

    public bool HasActiveWorkout => _exercises.Count > 0;

    public IReadOnlyList<WorkoutExerciseVm> Exercises => _exercises;

    public string? WorkoutNotes { get; set; }

    public bool HasInvalidSets() => _exercises.Any(exercise => exercise.Sets.Any(set => !IsValidSet(exercise, set)));

    public bool HasAnyValidSets() => _exercises.Any(exercise => exercise.Sets.Any(set => IsValidSet(exercise, set)));

    public bool CanSaveWorkout() => HasActiveWorkout && !HasInvalidSets();

    public void AddExercise(ExerciseDto exercise)
    {
        if (_exercises.Count == 0)
        {
            StartTime = DateTime.UtcNow;
        }

        var workoutExercise = new WorkoutExerciseVm
        {
            ExerciseId = exercise.Id,
            ExerciseName = exercise.Name,
            MuscleGroup = exercise.MuscleGroup,
            Description = exercise.Description,
            DefaultRestSeconds = exercise.DefaultRestSeconds,
            IsTimed = exercise.IsTimed,
            Sets =
            [
                new ExerciseSetVm
                {
                    SetNumber = 1
                }
            ]
        };
        
        _exercises.Add(workoutExercise);
        NotifyChange();
    }

    public void LoadWorkoutForEditing(WorkoutDetailDto workout)
    {
        _exercises.Clear();
        WorkoutNotes = workout.Notes;

        foreach (var exercise in workout.Exercises.OrderBy(exercise => exercise.Order))
        {
            _exercises.Add(new WorkoutExerciseVm
            {
                ExerciseId = exercise.ExerciseId,
                ExerciseName = exercise.ExerciseName,
                MuscleGroup = exercise.MuscleGroup,
                Description = exercise.ExerciseDescription,
                DefaultRestSeconds = exercise.DefaultRestSeconds,
                IsTimed = exercise.IsTimed,
                Notes = exercise.Notes,
                Sets = exercise.Sets
                    .OrderBy(set => set.SetNumber)
                    .Select(set => new ExerciseSetVm
                    {
                        SetNumber = set.SetNumber,
                        Reps = set.Reps,
                        DurationSeconds = set.DurationSeconds,
                        Weight = set.Weight,
                        RIR = set.RIR,
                        Notes = set.Notes,
                        IsCompleted = true
                    })
                    .ToList()
            });
        }

        NotifyChange();
    }

    public void RemoveExercise(int exerciseIndex)
    {
        if (exerciseIndex < 0 || exerciseIndex >= _exercises.Count)
        {
            return;
        }

        _exercises.RemoveAt(exerciseIndex);
        
        NotifyChange();
    }

    public void AddSet(int exerciseIndex)
    {
        if (!TryGetExercise(exerciseIndex, out var workoutExercise))
        {
            return;
        }

        var nextSetNumber = workoutExercise.Sets.Count + 1;

        var newSet = new ExerciseSetVm
        {
            SetNumber = nextSetNumber
        };

        if (workoutExercise.Sets.Count > 0)
        {
            var previousSet = workoutExercise.Sets[^1];
            previousSet.IsCompleted = true;

            newSet.Weight = previousSet.Weight;
            newSet.Reps = previousSet.Reps;
            newSet.RIR = previousSet.RIR;
        }

        workoutExercise.Sets.Add(newSet);

        NotifyChange();
    }
    
    public void RemoveSet(int exerciseIndex, int setIndex)
    {
        if (!TryGetExercise(exerciseIndex, out var workoutExercise))
        {
            return;
        }

        if (setIndex < 0 || setIndex >= workoutExercise.Sets.Count)
        {
            return;
        }

        workoutExercise.Sets.RemoveAt(setIndex);
        RenumberSets(workoutExercise);

        NotifyChange();
    }
    
    public void UpdateSet(int exerciseIndex, int setIndex, ExerciseSetVm updatedSet)
    {
        if (!TryGetExercise(exerciseIndex, out var workoutExercise))
        {
            return;
        }

        if (setIndex < 0 || setIndex >= workoutExercise.Sets.Count)
        {
            return;
        }

        var existingSet = workoutExercise.Sets[setIndex];

        existingSet.Reps = updatedSet.Reps;
        existingSet.DurationSeconds = updatedSet.DurationSeconds;
        existingSet.Weight = updatedSet.Weight;
        existingSet.RIR = updatedSet.RIR;
        existingSet.IsCompleted = updatedSet.IsCompleted;
        existingSet.Notes = updatedSet.Notes;

        NotifyChange();
    }

    public void CompleteWorkoutFinalSet()
    {
        if (_exercises.Count == 0)
        {
            return;
        }

        var workoutExercise = _exercises[^1];

        if (workoutExercise.Sets.Count > 0)
        {
            workoutExercise.Sets[^1].IsCompleted = true;
        }

        NotifyChange();
    }

    public void UpdateExerciseDescription(int exerciseIndex, string? description)
    {
        if (!TryGetExercise(exerciseIndex, out var workoutExercise))
        {
            return;
        }

        workoutExercise.Description = description;
        NotifyChange();
    }

    public void Clear()
    {
        _exercises.Clear();
        WorkoutNotes = null;
        NotifyChange();
    }
    
    public WorkoutCreateDto ToCreateDto(DateTime? dateOverride = null, int? durationMinutesOverride = null)
    {
        var dto = new WorkoutCreateDto
        {
            Date = dateOverride ?? StartTime.Date,
            Notes = WorkoutNotes,
            DurationMinutes = durationMinutesOverride
                ?? (HasActiveWorkout
                    ? Math.Max(1, (int)Math.Ceiling((DateTime.UtcNow - StartTime).TotalMinutes))
                    : null)
        };

        foreach (var exercise in _exercises)
        {
            var workoutExercise = new WorkoutExerciseCreateDto
            {
                ExerciseId = exercise.ExerciseId,
                Notes = exercise.Notes,
                ExerciseDescription = exercise.Description
            };

            foreach (var set in exercise.Sets)
            {
                workoutExercise.Sets.Add(new ExerciseSetCreateDto
                {
                    SetNumber = set.SetNumber,
                    Reps = set.Reps,
                    DurationSeconds = set.DurationSeconds,
                    Weight = set.Weight,
                    RIR = set.RIR,
                    Notes = set.Notes
                });
            }

            dto.Exercises.Add(workoutExercise);
        }

        return dto;
    }
    
    private void NotifyChange() => OnChange?.Invoke();

    private static bool IsValidSet(WorkoutExerciseVm exercise, ExerciseSetVm set)
    {
        return exercise.IsTimed
            ? set.DurationSeconds.GetValueOrDefault() > 0
            : set.Reps.GetValueOrDefault() > 0 || set.Weight > 0m;
    }
    
    private static void RenumberSets(WorkoutExerciseVm workoutExercise)
    {
        for (var i = 0; i < workoutExercise.Sets.Count; i++)
        {
            workoutExercise.Sets[i].SetNumber = i + 1;
        }
    }
    
    private bool TryGetExercise(int exerciseIndex, out WorkoutExerciseVm workoutExercise)
    {
        if (exerciseIndex >= 0 && exerciseIndex < _exercises.Count)
        {
            workoutExercise = _exercises[exerciseIndex];
            return true;
        }

        workoutExercise = null!;
        return false;
    }
}