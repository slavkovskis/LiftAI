using LiftAI.Api.Data.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LiftAI.Api.Data;
using LiftAI.Shared.Models;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Workout> Workouts => Set<Workout>();
    public DbSet<Exercise> Exercises => Set<Exercise>();
    public DbSet<WorkoutExercise> WorkoutExercises => Set<WorkoutExercise>();
    public DbSet<ExerciseSet> ExerciseSets => Set<ExerciseSet>();
    public DbSet<ChatConversation> ChatConversations => Set<ChatConversation>();
    public DbSet<ChatMessageEntity> ChatMessages => Set<ChatMessageEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User → Workout
        modelBuilder.Entity<Workout>()
            .HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(w => w.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Exercise (system + user)
        modelBuilder.Entity<Exercise>()
            .HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        // Workout → WorkoutExercise
        modelBuilder.Entity<WorkoutExercise>()
            .HasOne(we => we.Workout)
            .WithMany(w => w.Exercises)
            .HasForeignKey(we => we.WorkoutId)
            .OnDelete(DeleteBehavior.Cascade);

        // WorkoutExercise → Exercise
        modelBuilder.Entity<WorkoutExercise>()
            .HasOne(we => we.Exercise)
            .WithMany()
            .HasForeignKey(we => we.ExerciseId)
            .OnDelete(DeleteBehavior.Restrict);

        // WorkoutExercise → ExerciseSet
        modelBuilder.Entity<ExerciseSet>()
            .HasOne(es => es.WorkoutExercise)
            .WithMany(we => we.Sets)
            .HasForeignKey(es => es.WorkoutExerciseId)
            .OnDelete(DeleteBehavior.Cascade);
        
        modelBuilder.Entity<ChatConversation>()
            .HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ChatMessageEntity>()
            .HasOne(m => m.Conversation)
            .WithMany(c => c.Messages)
            .HasForeignKey(m => m.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        // Performance indexes
        modelBuilder.Entity<Workout>()
            .HasIndex(w => new { w.UserId, w.Date });

        modelBuilder.Entity<Exercise>()
            .HasIndex(e => new { e.UserId, e.Name });
        
        modelBuilder.Entity<ChatConversation>()
            .HasIndex(c => new { c.UserId, c.LastMessageAt });

        modelBuilder.Entity<ChatMessageEntity>()
            .HasIndex(m => new { m.ConversationId, m.CreatedAt });
        
        modelBuilder.Entity<ChatMessageEntity>()
            .Property(m => m.Role)
            .HasConversion<string>();
    }
}