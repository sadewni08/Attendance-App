using ChronoTrack.DTOs;
using ChronoTrack.DTOs.Common;
using Microsoft.AspNetCore.Mvc;
using ChronoTrack.Services.Interfaces;
using ChronoTrack.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace ChronoTrack.Endpoints
{
    public static class UserEndpoints
    {
        public static void MapUserEndpoints(this IEndpointRouteBuilder routes)
        {
            var employeeApi = routes.MapGroup("/api/employees").WithTags("Employees");

            // Create a new employee
            employeeApi.MapPost("/", async ([FromBody] CreateUserDto command, IUserService service, ILogger<UserService> logger) =>
            {
                try 
                {
                    logger.LogInformation("Received request to create employee: {UserId}, {FirstName} {LastName}", 
                        command.UserId, command.FirstName, command.LastName);
                        
                    // Log all important values including contact details
                    logger.LogInformation("Request details: Department: {DepartmentID}, Role: {UserRoleID}", 
                        command.DepartmentID, command.UserRoleID);
                    logger.LogInformation("Contact details: Email: {Email}, Phone: {Phone}, Address: {Address}", 
                        command.EmailAddress, command.PhoneNumber, command.Address);
                    
                    var result = await service.CreateUserAsync(command);
                    
                    if (result.Success)
                    {
                        logger.LogInformation("Employee created successfully: {Id}", result.Data?.Id);
                        return Results.Created($"/api/employees/{result.Data?.Id}", result);
                    }
                    else
                    {
                        logger.LogWarning("Failed to create employee: {Message}", result.Message);
                        return Results.BadRequest(result);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error creating employee");
                    return Results.BadRequest(new ApiResponse<object>(false, $"Error: {ex.Message}"));
                }
            })
            .WithName("CreateEmployee")
            .WithDescription("Creates a new employee")
            .WithOpenApi();

            // Get all employees with pagination
            employeeApi.MapGet("/", async (IUserService service, int page = 1, int pageSize = 10, ILogger<UserService> logger = null) =>
            {
                try
                {
                    logger?.LogInformation("Getting all employees - Page: {Page}, PageSize: {PageSize}", page, pageSize);
                    
                    // Validate pagination parameters
                    if (page < 1)
                    {
                        page = 1;
                        logger?.LogWarning("Invalid page number provided, defaulting to page 1");
                    }
                    
                    if (pageSize < 1 || pageSize > 100)
                    {
                        pageSize = 10;
                        logger?.LogWarning("Invalid page size provided, defaulting to 10");
                    }
                    
                    var result = await service.GetAllUsersAsync(page, pageSize);
                    
                    if (result.Success)
                    {
                        // Ensure pagination data is properly set
                        if (result.Data != null)
                        {
                            // Instead of modifying init-only properties, create a new PagedResponse
                            // with the correct values and update the ApiResponse's Data property
                            var updatedData = new PagedResponse<UserSummaryDto>(
                                result.Data.Items,
                                page,
                                pageSize,
                                result.Data.TotalItems,
                                result.Data.TotalPages
                            );
                            
                            // Create a new API response with the updated PagedResponse
                            result = new ApiResponse<PagedResponse<UserSummaryDto>>(
                                result.Success,
                                result.Message,
                                updatedData
                            );
                            
                            logger?.LogInformation("Successfully retrieved {Count} employees out of {Total} total employees", 
                                result.Data.Items?.Count ?? 0, result.Data.TotalItems);
                            logger?.LogInformation("Pagination info - Page: {Page}, Total Pages: {TotalPages}, Start: {Start}, End: {End}", 
                                result.Data.Page, result.Data.TotalPages, result.Data.StartItem, result.Data.EndItem);
                        }
                        
                        return Results.Ok(result);
                    }
                    else
                    {
                        logger?.LogWarning("Failed to retrieve employees: {Message}", result.Message);
                        return Results.BadRequest(result);
                    }
                }
                catch (Exception ex)
                {
                    logger?.LogError(ex, "Error retrieving employees");
                    return Results.BadRequest(new ApiResponse<object>(false, $"Error: {ex.Message}"));
                }
            })
            .WithName("GetAllEmployees")
            .WithDescription("Gets all employees with pagination")
            .WithOpenApi();

            // Get employee by ID
            employeeApi.MapGet("/{userId}", async (IUserService service, string userId, ILogger<UserService> logger = null) =>
            {
                try
                {
                    logger?.LogInformation("Getting employee by UserID: {UserId}", userId);
                    
                    var result = await service.GetUserByUserIdAsync(userId);
                    
                    if (result.Success && result.Data != null)
                    {
                        var user = result.Data;
                        logger?.LogInformation("Employee found: {UserId}, {FirstName} {LastName}, Gender: {Gender}", 
                            userId, user.FirstName, user.LastName, user.Gender);
                        
                        // Log contact details being returned
                        logger?.LogInformation("Employee contact details: Email: {Email}, Phone: {Phone}, Address: {Address}", 
                            user.EmailAddress, user.PhoneNumber, user.Address);
                            
                        return Results.Ok(result);
                    }
                    else
                    {
                        logger?.LogWarning("Employee not found: {UserId}", userId);
                        return Results.NotFound(result);
                    }
                }
                catch (Exception ex)
                {
                    logger?.LogError(ex, "Error retrieving employee {UserId}", userId);
                    return Results.BadRequest(new ApiResponse<object>(false, $"Error: {ex.Message}"));
                }
            })
            .WithName("GetEmployeeByUserId")
            .WithDescription("Gets an employee by UserId")
            .WithOpenApi();

            // Update employee
            employeeApi.MapPut("/{userId}", async (string userId, [FromBody] UpdateUserDto command, IUserService service, ILogger<UserService> logger = null) =>
            {
                try 
                {
                    logger?.LogInformation("Updating employee: {UserId}, {FirstName} {LastName}", 
                        userId, command.FirstName, command.LastName);
                        
                    // Log contact details being updated
                    logger?.LogInformation("Updating contact details: Email: {Email}, Phone: {Phone}, Address: {Address}", 
                        command.EmailAddress, command.PhoneNumber, command.Address);
                        
                    var result = await service.UpdateUserByUserIdAsync(userId, command);
                    
                    if (result.Success)
                    {
                        logger?.LogInformation("Employee updated successfully: {UserId}", userId);
                        return Results.Ok(result);
                    }
                    else
                    {
                        logger?.LogWarning("Failed to update employee: {Message}", result.Message);
                        return Results.BadRequest(result);
                    }
                }
                catch (Exception ex)
                {
                    logger?.LogError(ex, "Error updating employee");
                    return Results.BadRequest(new ApiResponse<object>(false, $"Error: {ex.Message}"));
                }
            })
            .WithName("UpdateEmployee")
            .WithDescription("Updates an existing employee")
            .WithOpenApi();

            // Delete employee
            employeeApi.MapDelete("/{userId}", async (string userId, IUserService service, ILogger<UserService> logger = null) =>
            {
                try
                {
                    // Log the deletion attempt
                    logger?.LogInformation("Attempting to delete employee with UserID: {UserId}", userId);
                    
                    // Get the employee details before deletion for logging purposes
                    var employee = await service.GetUserByUserIdAsync(userId);
                    if (employee.Success && employee.Data != null)
                    {
                        logger?.LogInformation("Deleting employee: {UserId}, {FirstName} {LastName}, Email: {Email}", 
                            userId, employee.Data.FirstName, employee.Data.LastName, employee.Data.EmailAddress);
                    }
                    
                    var result = await service.DeleteUserByUserIdAsync(userId);
                    
                    if (result.Success)
                    {
                        logger?.LogInformation("Employee deleted successfully: {UserId}", userId);
                        return Results.Ok(result);
                    }
                    else
                    {
                        logger?.LogWarning("Failed to delete employee: {UserId}, {Message}", userId, result.Message);
                        return Results.BadRequest(result);
                    }
                }
                catch (Exception ex)
                {
                    logger?.LogError(ex, "Error deleting employee {UserId}", userId);
                    return Results.BadRequest(new ApiResponse<object>(false, $"Error: {ex.Message}"));
                }
            })
            .WithName("DeleteEmployee")
            .WithDescription("Deletes an employee")
            .WithOpenApi();

            // Get employee count
            employeeApi.MapGet("/count", async (IUserService service, ILogger<UserService> logger = null) =>
            {
                try
                {
                    logger?.LogInformation("Getting total employee count");
                    
                    var result = await service.GetAllUsersAsync(1, 1);
                    
                    int totalEmployees = result.Data?.TotalItems ?? 0;
                    logger?.LogInformation("Total employees count: {Count}", totalEmployees);
                    
                    // Return in the standard ApiResponse format for consistency
                    return Results.Ok(new ApiResponse<int>(true, "Employee count retrieved successfully", totalEmployees));
                }
                catch (Exception ex)
                {
                    logger?.LogError(ex, "Error retrieving employee count");
                    return Results.BadRequest(new ApiResponse<int>(false, $"Error: {ex.Message}", 0));
                }
            })
            .WithName("GetEmployeeCount")
            .WithDescription("Gets the total number of employees")
            .WithOpenApi();

            // Get employees by department
            employeeApi.MapGet("/department/{departmentId}", async (IUserService service, Guid departmentId, int page = 1, int pageSize = 10, ILogger<UserService> logger = null) =>
            {
                try
                {
                    logger?.LogInformation("Getting employees by department ID: {DepartmentId}, Page: {Page}, PageSize: {PageSize}", 
                        departmentId, page, pageSize);
                    
                    var result = await service.GetUsersByDepartmentAsync(departmentId, page, pageSize);
                    
                    if (result.Success)
                    {
                        logger?.LogInformation("Successfully retrieved {Count} employees for department {DepartmentId}", 
                            result.Data?.Items?.Count ?? 0, departmentId);
                        
                        // Log some details about the returned data
                        if (result.Data?.Items != null && logger != null)
                        {
                            foreach (var user in result.Data.Items)
                            {
                                logger.LogDebug("Employee: {UserId}, Name: {FirstName} {LastName}, Gender: {Gender}, Email: {Email}, Phone: {Phone}, Address: {Address}", 
                                    user.UserId, user.FirstName, user.LastName, user.Gender, user.EmailAddress, user.PhoneNumber, user.Address);
                            }
                        }
                        
                        return Results.Ok(result);
                    }
                    else
                    {
                        logger?.LogWarning("Failed to retrieve employees by department: {Message}", result.Message);
                        return Results.BadRequest(result);
                    }
                }
                catch (Exception ex)
                {
                    logger?.LogError(ex, "Error retrieving employees by department {DepartmentId}", departmentId);
                    return Results.BadRequest(new ApiResponse<object>(false, $"Error: {ex.Message}"));
                }
            })
            .WithName("GetEmployeesByDepartment")
            .WithDescription("Gets employees by department")
            .WithOpenApi();
        }
    }
}