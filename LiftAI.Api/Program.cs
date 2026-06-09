using LiftAI.Api.Data;
using LiftAI.Api.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.AddLiftAiDb();
builder.AddJwtAuthentication();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazorApp", policy =>
        policy.WithOrigins("http://localhost:5187", "https://localhost:7222")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials());
});

var app = builder.Build();

app.UseCors("AllowBlazorApp");
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapExerciseEndpoints();
app.MapWorkoutEndpoints();
app.MapAuthEndpoints();
app.MapChatEndpoints();
app.MapChatConversationEndpoints();

app.MigrateDb();

app.Run();