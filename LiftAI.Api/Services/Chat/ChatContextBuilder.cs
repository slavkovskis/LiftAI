using System.Globalization;
using System.Text;
using LiftAI.Api.Data;
using LiftAI.Shared.Models.Dtos.Chat;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LiftAI.Api.Services.Chat;

public class ChatContextBuilder(ApplicationDbContext db, IOptions<ChatOptions> options)
{
    private readonly ChatOptions _options = options.Value;

    public async Task<string> BuildSystemPromptAsync(
        string userId,
        ContextMode mode,
        int conversationId,
        CancellationToken ct = default)
    {
        var workoutLimit = mode == ContextMode.Deep
            ? _options.DeepWorkoutLimit
            : _options.NormalWorkoutLimit;

        var workouts = await db.Workouts
            .AsNoTracking()
            .Where(w => w.UserId == userId)
            .OrderByDescending(w => w.Date)
            .Take(workoutLimit)
            .Include(w => w.Exercises)
            .ThenInclude(we => we.Exercise)
            .Include(w => w.Exercises)
            .ThenInclude(we => we.Sets)
            .AsSplitQuery()
            .ToListAsync(ct);

        var recentMessages = await db.ChatMessages
            .AsNoTracking()
            .Where(m => m.ConversationId == conversationId)
            .OrderByDescending(m => m.CreatedAt)
            .Take(_options.MaxPersistedMessagesForPrompt)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync(ct);

        var sb = new StringBuilder();

        sb.AppendLine("You are LiftAI, a concise fitness assistant.");
        sb.AppendLine("Use the user's recent workout and chat history as context.");
        sb.AppendLine("Do not provide medical diagnosis. Recommend professional help for injury or pain.");
        sb.AppendLine("When answering, only use the most recent workout date shown below unless explicitly asked to compare multiple workouts.");
        sb.AppendLine("Treat the latest date as the ‘current’ workout and ignore older workouts unless the user asks for history.");
        sb.AppendLine("Do not use workout ids or any kind of backend knowledge that the user should not need to know. Like workout id, exercise id etc.");
        sb.AppendLine("Respond in plain text only.");
        sb.AppendLine("Do not use Markdown formatting, including bold, italics, bullet styling, code blocks, or tables.");
        sb.AppendLine("Write natural language answers without special formatting.");
        sb.AppendLine();

        sb.AppendLine($"Context mode: {mode}");
        sb.AppendLine($"Recent workouts included: {workouts.Count}");
        sb.AppendLine();

        if (workouts.Count == 0)
        {
            sb.AppendLine("No workout history available yet.");
        }
        else
        {
            sb.AppendLine("Workout history:");
            foreach (var workout in workouts)
            {
                var date = workout.Date.ToString("yyyy-MM-dd");
                sb.AppendLine($"- {date}:");

                if (!string.IsNullOrWhiteSpace(workout.Notes))
                    sb.AppendLine($"  Workout notes: {workout.Notes}");

                foreach (var exercise in workout.Exercises.OrderBy(e => e.Order))
                {
                    sb.AppendLine($"  - {exercise.Exercise.Name}");

                    if (!string.IsNullOrWhiteSpace(exercise.Notes))
                        sb.AppendLine($"    Exercise notes: {exercise.Notes}");

                    foreach (var set in exercise.Sets.OrderBy(s => s.SetNumber))
                    {
                        var durationInSeconds = set.DurationSeconds?.ToString() ?? "N/A";
                        var reps = set.Reps?.ToString() ?? "N/A";
                        var rir = set.RIR?.ToString() ?? "N/A";
                        var weight = set.Weight;

                        sb.AppendLine($"    Set {set.SetNumber}: weight {(weight > 0 ? weight.ToString(CultureInfo.CurrentCulture) : "Just machine weight")} kg, reps/duration in seconds {(exercise.Exercise.IsTimed ? durationInSeconds : reps)}, RIR {rir}");

                        if (!string.IsNullOrWhiteSpace(set.Notes))
                            sb.AppendLine($"    Set notes: {set.Notes}");
                    }
                }
            }
        }

        sb.AppendLine();
        sb.AppendLine("Recent conversation history:");

        if (recentMessages.Count == 0)
        {
            sb.AppendLine("- No previous messages.");
        }
        else
        {
            foreach (var message in recentMessages)
            {
                sb.AppendLine($"- {message.Role}: {message.Content}");
            }
        }

        return sb.ToString();
    }
}