using LiftAI.Api.Data;
using LiftAI.Shared.Models.Dtos;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;
using LiftAI.Api.Data.Models;
using LiftAI.Api.Services.Email;
using LiftAI.Shared.Models.Dtos.Email;

namespace LiftAI.Api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/auth");

        // REGISTER
        group.MapPost("/register", async (RegisterDto dto, UserManager<ApplicationUser> userManager, ApplicationDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(dto.Email))
            {
                return Results.BadRequest(new AuthResponseDto { Success = false, Message = "Email is required." });
            }
            
            var user = new ApplicationUser
            {
                UserName = dto.Email,
                Email = dto.Email,
                FullName = dto.FullName,
                IsPremium = false
            };

            var result = await userManager.CreateAsync(user, dto.Password);

            if (result.Succeeded)
            {
                DbInitializer.SeedDefaultExercisesForUser(db, user.Id);
                await db.SaveChangesAsync();

                return Results.Ok(new AuthResponseDto 
                { 
                    Success = true, 
                    Message = "User registered successfully" 
                });
            }

            return Results.BadRequest(new AuthResponseDto 
            { 
                Success = false, 
                Message = string.Join(", ", result.Errors.Select(e => e.Description)) 
            });
        })
        .WithTags("Auth");

        // LOGIN
        group.MapPost("/login", async (LoginDto dto, UserManager<ApplicationUser> userManager, IConfiguration config) =>
        {
            var user = await userManager.FindByEmailAsync(dto.Email);
            if (user == null)
                return Results.BadRequest(new AuthResponseDto { Success = false, Message = "Invalid credentials" });

            var isValidPassword = await userManager.CheckPasswordAsync(user, dto.Password);
            if (!isValidPassword)
                return Results.BadRequest(new AuthResponseDto { Success = false, Message = "Invalid credentials" });

            // Generate JWT Token
            var token = GenerateJwtToken(user, config);

            return Results.Ok(new AuthResponseDto 
            { 
                Success = true, 
                Token = token,
                Email = user.Email,
                IsPremium = user.IsPremium,
                Message = "Login successful"
            });
        })
        .WithTags("Auth");

        //FORGOT PASSWORD
        group.MapPost("/forgot-password", async (
                ForgotPasswordRequestDto dto,
                UserManager<ApplicationUser> userManager,
                IEmailSender emailSender,
                IConfiguration config) =>
            {
                if (string.IsNullOrWhiteSpace(dto.Email))
                    return Results.Ok(new AuthResponseDto { Success = true, Message = "If the email exists, a reset link was sent." });

                var user = await userManager.FindByEmailAsync(dto.Email);
                
                if (user != null)
                {
                    var token = await userManager.GeneratePasswordResetTokenAsync(user);
                    var encodedToken = WebUtility.UrlEncode(token);

                    var appBaseUrl = config["App:BaseUrl"] ?? "https://localhost:5001";
                    var resetLink = $"{appBaseUrl}/reset-password?email={WebUtility.UrlEncode(user.Email)}&token={encodedToken}";

                    var body = $@"
                        <p>You requested a password reset.</p>
                        <p><a href=""{resetLink}"">Click here to reset your password</a></p>";

                    await emailSender.SendAsync(user.Email!, "Reset your LiftAI password", body);
                }

                return Results.Ok(new AuthResponseDto { Success = true, Message = "If the email exists, a reset link was sent." });
            })
            .WithTags("Auth");
        
        //RESET PASSWORD
        group.MapPost("/reset-password", async (
            ResetPasswordRequestDto dto,
            UserManager<ApplicationUser> userManager) =>
        {
            if (string.IsNullOrWhiteSpace(dto.Email) ||
                string.IsNullOrWhiteSpace(dto.Token) ||
                string.IsNullOrWhiteSpace(dto.NewPassword))
            {
                return Results.BadRequest(new AuthResponseDto { Success = false, Message = "Invalid reset request." });
            }

            var user = await userManager.FindByEmailAsync(dto.Email);
            if (user == null)
                return Results.BadRequest(new AuthResponseDto { Success = false, Message = "Invalid reset request." });

            var result = await userManager.ResetPasswordAsync(user, dto.Token, dto.NewPassword);

            if (!result.Succeeded)
                return Results.BadRequest(new AuthResponseDto
                {
                    Success = false,
                    Message = string.Join(", ", result.Errors.Select(e => e.Description))
                });

            return Results.Ok(new AuthResponseDto { Success = true, Message = "Password reset successful." });
        })
        .WithTags("Auth");
    }

    private static string GenerateJwtToken(ApplicationUser user, IConfiguration config)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Email, user.Email!),
            new Claim(ClaimTypes.Name, user.FullName ?? ""),
            new Claim("IsPremium", user.IsPremium.ToString())
        };

        var key = new SymmetricSecurityKey(config["Jwt:Key"] != null
            ? Encoding.UTF8.GetBytes(config["Jwt:Key"] ?? string.Empty)
            : throw new InvalidOperationException("JWT key not found in configuration"));
        
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            expires: DateTime.Now.AddDays(7),
            claims: claims,
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}