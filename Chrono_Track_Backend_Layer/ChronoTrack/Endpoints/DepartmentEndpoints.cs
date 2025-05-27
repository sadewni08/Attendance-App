using ChronoTrack.DTOs;
using ChronoTrack.Services.Interfaces;

namespace ChronoTrack.Endpoints
{
    public static class DepartmentEndpoints
    {
        public static void MapDepartmentEndpoints(this IEndpointRouteBuilder routes)
        {
            var departmentApi = routes.MapGroup("/api/departments").WithTags("Departments");

            // Get all departments
            departmentApi.MapGet("/", async (IDepartmentService service, int page = 1, int pageSize = 10) =>
            {
                var result = await service.GetAllDepartmentsAsync(page, pageSize);
                return result.Success ? Results.Ok(result) : Results.BadRequest(result);
            })
            .WithName("GetAllDepartments")
            .WithDescription("Gets all departments with pagination")
            .WithOpenApi();

            // Get department by ID
            departmentApi.MapGet("/{id}", async (IDepartmentService service, Guid id) =>
            {
                var result = await service.GetDepartmentByIdAsync(id);
                return result.Success ? Results.Ok(result) : Results.NotFound(result);
            })
            .WithName("GetDepartmentById")
            .WithDescription("Gets a department by ID")
            .WithOpenApi();

            // Create department
            departmentApi.MapPost("/", async (IDepartmentService service, CreateDepartmentDto command) =>
            {
                var result = await service.CreateDepartmentAsync(command);
                return result.Success
                    ? Results.Created($"/api/departments/{result.Data?.Id}", result)
                    : Results.BadRequest(result);
            })
            .WithName("CreateDepartment")
            .WithDescription("Creates a new department")
            .WithOpenApi();

            // Update department
            departmentApi.MapPut("/{id}", async (IDepartmentService service, Guid id, UpdateDepartmentDto command) =>
            {
                var result = await service.UpdateDepartmentAsync(id, command);
                return result.Success ? Results.Ok(result) : Results.BadRequest(result);
            })
            .WithName("UpdateDepartment")
            .WithDescription("Updates an existing department")
            .WithOpenApi();

            // Delete department
            departmentApi.MapDelete("/{id}", async (IDepartmentService service, Guid id) =>
            {
                var result = await service.DeleteDepartmentAsync(id);
                return result.Success ? Results.NoContent() : Results.BadRequest(result);
            })
            .WithName("DeleteDepartment")
            .WithDescription("Deletes a department")
            .WithOpenApi();

            // Get active departments
            departmentApi.MapGet("/active", async (IDepartmentService service) =>
            {
                var result = await service.GetActiveDepartmentsAsync();
                return result.Success ? Results.Ok(result) : Results.BadRequest(result);
            })
            .WithName("GetActiveDepartments")
            .WithDescription("Gets all active departments")
            .WithOpenApi();

            // Get department summary
            departmentApi.MapGet("/{id}/summary", async (IDepartmentService service, Guid id, DateTime? fromDate, DateTime? toDate) =>
            {
                var result = await service.GetDepartmentSummaryAsync(id, fromDate, toDate);
                return result.Success ? Results.Ok(result) : Results.NotFound(result);
            })
            .WithName("GetDepartmentSummary")
            .WithDescription("Gets a department's summary including attendance statistics")
            .WithOpenApi();

            // Get all departments summary
            departmentApi.MapGet("/summary", async (IDepartmentService service, DateTime? fromDate, DateTime? toDate) =>
            {
                var result = await service.GetAllDepartmentsSummaryAsync(fromDate, toDate);
                return result.Success ? Results.Ok(result) : Results.BadRequest(result);
            })
            .WithName("GetAllDepartmentsSummary")
            .WithDescription("Gets summary for all departments including attendance statistics")
            .WithOpenApi();
        }
    }
} 