using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using LiftAI.App;
using LiftAI.App.Auth;
using LiftAI.App.Services;
using Microsoft.AspNetCore.Components.Authorization;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped<AuthTokenHandler>();

builder.Services.AddHttpClient("Api", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ApiBaseUrl"] ?? "https://localhost:7168");
})
.AddHttpMessageHandler<AuthTokenHandler>();

builder.Services.AddScoped<ExerciseService>();
builder.Services.AddScoped<WorkoutService>();
builder.Services.AddScoped<ActiveWorkoutState>();

builder.Services.AddScoped<ChatService>();
builder.Services.AddScoped<ChatHistoryState>();

builder.Services.AddScoped<JwtAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp =>sp.GetRequiredService<JwtAuthenticationStateProvider>());

builder.Services.AddAuthorizationCore();

await builder.Build().RunAsync();