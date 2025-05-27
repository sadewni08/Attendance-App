using ChronoTrack.DTOs;
using ChronoTrack.Services.Interfaces;

namespace ChronoTrack.Endpoints
{
    public static class UserRoleEndpoints
    {
        public static void MapUserRoleEndpoints(this IEndpointRouteBuilder routes)
        {
            var userRoleApi = routes.MapGroup("/api/userroles").WithTags("User Roles");

            // Get all user roles
            userRoleApi.MapGet("/", async (IUserRoleService service, int page = 1, int pageSize = 10) =>
            {
                var result = await service.GetAllUserRolesAsync(page, pageSize);
                return result.Success ? Results.Ok(result) : Results.BadRequest(result);
            })
            .WithName("GetAllUserRoles")
            .WithDescription("Gets all user roles with pagination")
            .WithOpenApi();

            // Get user role by ID
            userRoleApi.MapGet("/{id}", async (IUserRoleService service, Guid id) =>
            {
                var result = await service.GetUserRoleByIdAsync(id);
                return result.Success ? Results.Ok(result) : Results.NotFound(result);
            })
            .WithName("GetUserRoleById")
            .WithDescription("Gets a user role by ID")
            .WithOpenApi();

            // Create user role
            userRoleApi.MapPost("/", async (IUserRoleService service, CreateUserRoleDto command) =>
            {
                var result = await service.CreateUserRoleAsync(command);
                return result.Success
                    ? Results.Created($"/api/userroles/{result.Data?.Id}", result)
                    : Results.BadRequest(result);
            })
            .WithName("CreateUserRole")
            .WithDescription("Creates a new user role")
            .WithOpenApi();

            // Update user role
            userRoleApi.MapPut("/{id}", async (IUserRoleService service, Guid id, UpdateUserRoleDto command) =>
            {
                var result = await service.UpdateUserRoleAsync(id, command);
                return result.Success ? Results.Ok(result) : Results.BadRequest(result);
            })
            .WithName("UpdateUserRole")
            .WithDescription("Updates an existing user role")
            .WithOpenApi();

            // Delete user role
            userRoleApi.MapDelete("/{id}", async (IUserRoleService service, Guid id) =>
            {
                var result = await service.DeleteUserRoleAsync(id);
                return result.Success ? Results.NoContent() : Results.BadRequest(result);
            })
            .WithName("DeleteUserRole")
            .WithDescription("Deletes a user role")
            .WithOpenApi();

            // Get active user roles
            userRoleApi.MapGet("/active", async (IUserRoleService service) =>
            {
                var result = await service.GetActiveUserRolesAsync();
                return result.Success ? Results.Ok(result) : Results.BadRequest(result);
            })
            .WithName("GetActiveUserRoles")
            .WithDescription("Gets all active user roles")
            .WithOpenApi();
        }
    }
} 