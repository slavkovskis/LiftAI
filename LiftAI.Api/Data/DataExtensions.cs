using System.Text;
using LiftAI.Api.Data.Models;
using LiftAI.Api.Services.Chat;
using LiftAI.Api.Services.Conversation;
using LiftAI.Api.Services.Email;
using LiftAI.Api.Services.Ollama;
using LiftAI.Shared.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace LiftAI.Api.Data;

public static class DataExtensions
{
    public static void MigrateDb(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        dbContext.Database.Migrate();
    }
    
    extension(WebApplicationBuilder builder)
    {
        public void AddLiftAiDb()
        {
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

            builder.Services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseNpgsql(connectionString);

                options.UseSeeding((context, _) =>
                {
                    DbInitializer.Initialize((ApplicationDbContext)context);
                });
            });
        }

        public void AddJwtAuthentication()
        {
            builder.Services.Configure<ChatOptions>(builder.Configuration.GetSection("Chat"));
            builder.Services.Configure<OllamaOptions>(builder.Configuration.GetSection("Ollama"));
            builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection("Smtp"));

            builder.Services.AddScoped<ChatContextBuilder>();
            builder.Services.AddScoped<IConversationService, ConversationService>();
            builder.Services.AddHttpClient<IOllamaChatClient, OllamaChatClient>();
            builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();

            builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();
        
            builder.Services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = false,
                        ValidateAudience = false,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(
                            builder.Configuration["Jwt:Key"] != null
                                ? Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? string.Empty)
                                : throw new InvalidOperationException("JWT key not found in configuration"))
                    };
                });
        
            builder.Services.AddAuthorization();
        }
    }
}