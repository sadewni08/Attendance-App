using ChronoTrack.DTOs;
using ChronoTrack.Services.Interfaces;

namespace ChronoTrack.Endpoints
{
    public static class AuthEndpoints
    {
        public static void MapAuthEndpoints(this IEndpointRouteBuilder routes)
        {
            var authApi = routes.MapGroup("/api/auth").WithTags("Authentication");

            // Login endpoint
            authApi.MapPost("/login", async (IUserService userService, UserLoginDto loginDto) =>
            {
                var result = await userService.AuthenticateAsync(loginDto);
                return result.Success
                    ? Results.Ok(result)
                    : Results.BadRequest(result);
            })
            .WithName("Login")
            .WithDescription("Authenticates a user and returns a token")
            .WithOpenApi();
            
            // Check if user is registered by email
            authApi.MapGet("/check-email/{email}", async (IUserService userService, string email) =>
            {
                var result = await userService.IsUserRegisteredByEmailAsync(email);
                return result.Success
                    ? Results.Ok(result)
                    : Results.BadRequest(result);
            })
            .WithName("CheckEmail")
            .WithDescription("Checks if a user is registered with the provided email address")
            .WithOpenApi();
            
            // Microsoft authentication endpoint
            authApi.MapPost("/microsoft", async (IUserService userService, MicrosoftAuthDto authDto) =>
            {
                if (string.IsNullOrEmpty(authDto.Email))
                {
                    return Results.BadRequest(new { Success = false, Message = "Email is required" });
                }
                
                // First check if user is registered
                var isRegistered = await userService.IsUserRegisteredByEmailAsync(authDto.Email);
                if (!isRegistered.Success || !isRegistered.Data)
                {
                    return Results.BadRequest(new { Success = false, Message = "User with this email is not registered. Please contact your administrator." });
                }
                
                // Authenticate the user
                var result = await userService.AuthenticateByEmailAsync(authDto.Email);
                return result.Success
                    ? Results.Ok(result)
                    : Results.BadRequest(result);
            })
            .WithName("MicrosoftAuth")
            .WithDescription("Authenticates a user via Microsoft authentication")
            .WithOpenApi();
        }
    }
} 