// using System.Security.Cryptography.X509Certificates;
using LiftAI.Api.Data;
using LiftAI.Api.Endpoints;

var builder = WebApplication.CreateBuilder(args);

// Add database with seeding
builder.AddLiftAiDb();
builder.AddJwtAuthentication();

// builder.WebHost.ConfigureKestrel(options =>
// {
//     options.ListenAnyIP(7168, listenOptions =>
//     {
//         var certPath = Path.Combine(builder.Environment.ContentRootPath, "certs", "192.168.100.153+1.pem");
//         var keyPath = Path.Combine(builder.Environment.ContentRootPath, "certs", "192.168.100.153+1-key.pem");
//
//         var cert = X509Certificate2.CreateFromPemFile(certPath, keyPath);
//         listenOptions.UseHttps(cert);
//     });
//
//     options.ListenAnyIP(5250);
// });

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

// Migrate database and seed default exercises on startup
app.MigrateDb();

app.Run();