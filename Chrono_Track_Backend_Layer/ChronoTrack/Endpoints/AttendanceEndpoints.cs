using ChronoTrack.DTOs;
using ChronoTrack.Services.Interfaces;
using ChronoTrack.Services;
using ChronoTrack.DTOs.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using System.Text.Json;
using System.IO;

namespace ChronoTrack.Endpoints
    
{
    public static class AttendanceEndpoints
    {
        public static void MapAttendanceEndpoints(this IEndpointRouteBuilder routes)
        {
            var attendanceApi = routes.MapGroup("/api/attendance").WithTags("Attendance");

            // Check-in endpoint
            attendanceApi.MapPost("/{userId}/checkin", async (string userId, [FromBody] CreateAttendanceDto command, IAttendanceService service, ILogger<AttendanceService> logger, HttpContext context) =>
            {
                try
                {
                    // Log the raw request for debugging
                    string requestBody = "No data";
                    try
                    {
                        context.Request.EnableBuffering();
                        using (StreamReader reader = new StreamReader(context.Request.Body, leaveOpen: true))
                        {
                            requestBody = await reader.ReadToEndAsync();
                            context.Request.Body.Position = 0; // Reset so the framework can read it again
                        }
                        logger.LogInformation("Raw check-in request body: {RequestBody}", requestBody);
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning("Could not read request body: {Error}", ex.Message);
                    }

                    // Add detailed logging of all properties
                    logger.LogInformation("CheckIn Params => UserId: {UserId}, Date: {Date}, CheckInTime: {CheckInTime}, CheckOutTime: {CheckOutTime}",
                        userId, 
                        command.AttendanceDate, 
                        command.CheckInTime,
                        command.CheckOutTime);
                    
                    // Ensure the date is in UTC
                    var utcDate = DateTime.SpecifyKind(command.AttendanceDate, DateTimeKind.Utc);
                    
                    // Create a new command with UTC time
                    var utcCommand = new CreateAttendanceDto(
                        utcDate,
                        command.CheckInTime,
                        command.CheckOutTime
                    );
                    
                    var result = await service.CheckInAsync(userId, utcCommand);
                    
                    if (result.Success)
                    {
                        logger.LogInformation("Check-in successful for user: {UserId}", userId);
                        return Results.Ok(result);
                    }
                    else
                    {
                        logger.LogWarning("Check-in failed for user: {UserId}, Message: {Message}", userId, result.Message);
                        return Results.BadRequest(result);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error during check-in");
                    return Results.BadRequest(new ApiResponse<object>(false, $"Error during check-in: {ex.Message}"));
                }
            })
            .WithName("CheckIn")
            .WithDescription("Records attendance check-in for a user")
            .WithOpenApi();

            // Check-out endpoint
            attendanceApi.MapPut("/{userId}/checkout/{attendanceId}", async (string userId, string attendanceId, [FromBody] UpdateAttendanceDto command, IAttendanceService service, ILogger<AttendanceService> logger, HttpContext context) =>
            {
                try
                {
                    // Log the raw request for debugging
                    string requestBody = "No data";
                    try
                    {
                        context.Request.EnableBuffering();
                        using (StreamReader reader = new StreamReader(context.Request.Body, leaveOpen: true))
                        {
                            requestBody = await reader.ReadToEndAsync();
                            context.Request.Body.Position = 0; // Reset so the framework can read it again
                        }
                        logger.LogInformation("Raw check-out request body: {RequestBody}", requestBody);
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning("Could not read request body: {Error}", ex.Message);
                    }

                    logger.LogInformation("Attendance check-out request for user: {UserId}, attendance ID: {AttendanceId}, attendance date: {AttendanceDate}, CheckOutTime: {CheckOutTime}", 
                        userId, attendanceId, command.AttendanceDate, command.CheckOutTime);
                    
                    var result = await service.CheckOutAsync(userId, attendanceId, command);
                    
                    if (result.Success)
                    {
                        logger.LogInformation("Check-out successful for user: {UserId}, attendance ID: {AttendanceId}", userId, attendanceId);
                        return Results.Ok(result);
                    }
                    else
                    {
                        logger.LogWarning("Check-out failed for user: {UserId}, attendance ID: {AttendanceId}, Message: {Message}", 
                            userId, attendanceId, result.Message);
                        return Results.BadRequest(result);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error during check-out for user: {UserId}, attendance ID: {AttendanceId}", userId, attendanceId);
                    return Results.BadRequest(new ApiResponse<object>(false, $"Error during check-out: {ex.Message}"));
                }
            })
            .WithName("CheckOut")
            .WithDescription("Records attendance check-out for a user based on attendance ID")
            .WithOpenApi();

            // Get user attendance history
            attendanceApi.MapGet("/user/{userId}", async (
                IAttendanceService service,
                string userId,
                DateTime? startDate,
                DateTime? endDate,
                int page = 1,
                int pageSize = 10,
                ILogger<AttendanceService> logger = null) =>
            {
                try
                {
                    logger?.LogInformation("Getting attendance history for user: {UserId}, StartDate: {StartDate}, EndDate: {EndDate}", 
                        userId, startDate, endDate);
                    
                    // Ensure dates are in UTC
                    DateTime? utcStartDate = null;
                    if (startDate.HasValue)
                    {
                        utcStartDate = DateTime.SpecifyKind(startDate.Value, DateTimeKind.Utc);
                    }
                    
                    DateTime? utcEndDate = null;
                    if (endDate.HasValue)
                    {
                        utcEndDate = DateTime.SpecifyKind(endDate.Value, DateTimeKind.Utc);
                    }
                    
                    var result = await service.GetUserAttendanceHistoryAsync(userId, utcStartDate, utcEndDate, page, pageSize);
                    
                    if (result.Success)
                    {
                        logger?.LogInformation("Successfully retrieved attendance history for user: {UserId}, Records: {Count}", 
                            userId, result.Data?.Items?.Count ?? 0);
                        return Results.Ok(result);
                    }
                    else
                    {
                        logger?.LogWarning("Failed to retrieve attendance history for user: {UserId}, Message: {Message}", 
                            userId, result.Message);
                        return Results.BadRequest(result);
                    }
                }
                catch (Exception ex)
                {
                    logger?.LogError(ex, "Error retrieving attendance history for user: {UserId}", userId);
                    return Results.BadRequest(new ApiResponse<object>(false, $"Error retrieving attendance history: {ex.Message}"));
                }
            })
            .WithName("GetUserAttendanceHistory")
            .WithDescription("Gets attendance history for a user with optional date filtering")
            .WithOpenApi();

            // Get detailed attendance with filtering options
            attendanceApi.MapGet("/detailed", async (
                IAttendanceService service,
                string userId = null,
                string attendanceId = null,
                string employeeName = null,
                DateTime? startDate = null,
                DateTime? endDate = null,
                int page = 1,
                int pageSize = 10,
                bool debug = false,
                ILogger<AttendanceService> logger = null) =>
            {
                try
                {
                    logger?.LogInformation("Getting detailed attendance. UserId: {UserId}, AttendanceId: {AttendanceId}, EmployeeName: {EmployeeName}, StartDate: {StartDate}, EndDate: {EndDate}", 
                        userId, attendanceId, employeeName, startDate, endDate);
                    
                    // Ensure dates are in UTC
                    DateTime? utcStartDate = null;
                    if (startDate.HasValue)
                    {
                        utcStartDate = DateTime.SpecifyKind(startDate.Value, DateTimeKind.Utc);
                    }
                    
                    DateTime? utcEndDate = null;
                    if (endDate.HasValue)
                    {
                        utcEndDate = DateTime.SpecifyKind(endDate.Value, DateTimeKind.Utc);
                    }
                    
                    var result = await service.GetDetailedAttendanceAsync(userId, attendanceId, employeeName, utcStartDate, utcEndDate, page, pageSize);
                    
                    if (result.Success)
                    {
                        logger?.LogInformation("Successfully retrieved detailed attendance, Records: {Count}", 
                            result.Data?.Items?.Count ?? 0);
                        return Results.Ok(result);
                    }
                    else
                    {
                        logger?.LogWarning("Failed to retrieve detailed attendance, Message: {Message}", 
                            result.Message);
                        return Results.BadRequest(result);
                    }
                }
                catch (Exception ex)
                {
                    logger?.LogError(ex, "Error retrieving detailed attendance: {Error}", ex.Message);
                    return Results.BadRequest(new ApiResponse<object>(false, $"Error retrieving detailed attendance: {ex.Message}"));
                }
            })
            .WithName("GetDetailedAttendance")
            .WithDescription("Gets detailed attendance data with various filtering options")
            .WithOpenApi();

            // Daily attendance summary
            attendanceApi.MapGet("/daily-summary", async (
                IAttendanceService service, 
                DateTime date,
                ILogger<AttendanceService> logger = null) =>
            {
                try
                {
                    logger?.LogInformation("Getting daily attendance summary for date: {Date}", date);
                    
                    // Ensure date is in UTC
                    var utcDate = DateTime.SpecifyKind(date, DateTimeKind.Utc);
                    
                    var result = await service.GetDailyAttendanceSummaryAsync(utcDate);
                    
                    if (result.Success)
                    {
                        logger?.LogInformation("Successfully retrieved daily attendance summary for date: {Date}", date);
                        return Results.Ok(result);
                    }
                    else
                    {
                        logger?.LogWarning("Failed to retrieve daily attendance summary for date: {Date}, Message: {Message}", 
                            date, result.Message);
                        return Results.BadRequest(result);
                    }
                }
                catch (Exception ex)
                {
                    logger?.LogError(ex, "Error retrieving daily attendance summary for date: {Date}", date);
                    return Results.BadRequest(new ApiResponse<object>(false, $"Error retrieving daily summary: {ex.Message}"));
                }
            })
            .WithName("GetDailySummary")
            .WithDescription("Gets attendance summary for a specific date")
            .WithOpenApi();

            // Department attendance
            attendanceApi.MapGet("/department/{departmentId}", async (
                IAttendanceService service,
                Guid departmentId,
                DateTime date,
                int page = 1,
                int pageSize = 10,
                ILogger<AttendanceService> logger = null) =>
            {
                try
                {
                    logger?.LogInformation("Getting department attendance for department: {DepartmentId}, Date: {Date}", 
                        departmentId, date);
                    
                    // Ensure date is in UTC
                    var utcDate = DateTime.SpecifyKind(date, DateTimeKind.Utc);
                    
                    var result = await service.GetDepartmentAttendanceAsync(departmentId, utcDate, page, pageSize);
                    
                    if (result.Success)
                    {
                        logger?.LogInformation("Successfully retrieved department attendance, Records: {Count}", 
                            result.Data?.Items?.Count ?? 0);
                        return Results.Ok(result);
                    }
                    else
                    {
                        logger?.LogWarning("Failed to retrieve department attendance, Message: {Message}", result.Message);
                        return Results.BadRequest(result);
                    }
                }
                catch (Exception ex)
                {
                    logger?.LogError(ex, "Error retrieving department attendance for department: {DepartmentId}", departmentId);
                    return Results.BadRequest(new ApiResponse<object>(false, $"Error retrieving department attendance: {ex.Message}"));
                }
            })
            .WithName("GetDepartmentAttendance")
            .WithDescription("Gets attendance for all employees in a department for a specific date")
            .WithOpenApi();

            // Get user check-in/check-out status
            attendanceApi.MapGet("/{userId}/status", async (
                string userId, 
                IAttendanceService service, 
                ILogger<AttendanceService> logger) =>
            {
                try
                {
                    logger.LogInformation("Getting check-in/check-out status for user: {UserId}", userId);
                    
                    var result = await service.GetUserAttendanceStatusAsync(userId);
                    
                    if (result.Success)
                    {
                        logger.LogInformation("Successfully retrieved attendance status for user: {UserId}", userId);
                        return Results.Ok(result);
                    }
                    else
                    {
                        logger.LogWarning("Failed to retrieve attendance status for user: {UserId}, Message: {Message}", 
                            userId, result.Message);
                        return Results.BadRequest(result);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error retrieving attendance status for user: {UserId}", userId);
                    return Results.BadRequest(new ApiResponse<object>(false, $"Error retrieving attendance status: {ex.Message}"));
                }
            })
            .WithName("GetUserAttendanceStatus")
            .WithDescription("Gets the current check-in/check-out status for a user")
            .WithOpenApi();
            
            // New API for detailed check-in/check-out status
            attendanceApi.MapGet("/{userId}/checkin-checkout-status", async (
                string userId, 
                IAttendanceService service, 
                ILogger<AttendanceService> logger) =>
            {
                try
                {
                    logger.LogInformation("Getting detailed check-in/check-out status for user: {UserId}", userId);
                    
                    var result = await service.CheckInCheckOutStatusAsync(userId);
                    
                    if (result.Success)
                    {
                        logger.LogInformation("Successfully retrieved detailed check-in/check-out status for user: {UserId}", userId);
                        return Results.Ok(result);
                    }
                    else
                    {
                        logger.LogWarning("Failed to retrieve detailed check-in/check-out status for user: {UserId}, Message: {Message}", 
                            userId, result.Message);
                        return Results.BadRequest(result);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error retrieving detailed check-in/check-out status for user: {UserId}", userId);
                    return Results.BadRequest(new ApiResponse<object>(false, $"Error retrieving detailed check-in/check-out status: {ex.Message}"));
                }
            })
            .WithName("GetCheckInCheckOutStatus")
            .WithDescription("Gets detailed information about a user's check-in/check-out status")
            .WithOpenApi();
            
            // Get today's attendance statistics for dashboard
            attendanceApi.MapGet("/today-stats", async (
                IAttendanceService service,
                IUserService userService,
                ILogger<AttendanceService> logger) =>
            {
                try
                {
                    logger.LogInformation("Getting today's attendance statistics for dashboard");
                    
                    // Get the current date in Sri Lanka time zone
                    var sriLankaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Colombo");
                    var sriLankaDate = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, sriLankaTimeZone).Date;
                    
                    logger.LogInformation("Current date in Sri Lanka: {Date}", sriLankaDate);
                    
                    // Define the time thresholds (in Sri Lanka time)
                    var exactOnTimeThreshold = new TimeSpan(6, 0, 0); // 6:00 AM
                    var lateThreshold = new TimeSpan(6, 15, 0); // 6:15 AM
                    var absentThreshold = new TimeSpan(8, 0, 0); // 8:00 AM
                    
                    // Get all attendance records for today
                    var attendanceResult = await service.GetDetailedAttendanceAsync(
                        startDate: DateTime.SpecifyKind(sriLankaDate, DateTimeKind.Utc),
                        endDate: DateTime.SpecifyKind(sriLankaDate.AddDays(1).AddSeconds(-1), DateTimeKind.Utc),
                        pageSize: 1000 // Set a high limit to get all records
                    );
                    
                    // Get total employee count
                    var employeeResult = await userService.GetAllUsersAsync(1, 1);
                    int totalEmployees = employeeResult.Data?.TotalItems ?? 0;
                    
                    // Default counts
                    int totalArrivedCount = 0;
                    int onTimeCount = 0;
                    int lateArrivalsCount = 0;
                    int earlyDepartureCount = 0;
                    int absentCount = 0;
                    
                    if (attendanceResult.Success && attendanceResult.Data != null && attendanceResult.Data.Items != null)
                    {
                        var attendanceItems = attendanceResult.Data.Items;
                        logger.LogInformation("Retrieved {Count} attendance records for today", attendanceItems.Count);
                        
                        // Calculate the total count of employees who have arrived today
                        totalArrivedCount = attendanceItems.Count;
                        
                        // Check each attendance record and categorize
                        foreach (var attendance in attendanceItems)
                        {
                            // Convert check-in time to TimeSpan for comparison
                            if (attendance.CheckInTime != TimeSpan.Zero)
                            {
                                if (attendance.CheckInTime < exactOnTimeThreshold)
                                {
                                    // Before 6:00 AM - Early arrival
                                    earlyDepartureCount++;
                                }
                                else if (attendance.CheckInTime == exactOnTimeThreshold)
                                {
                                    // Exactly 6:00 AM - On time
                                    onTimeCount++;
                                }
                                else if (attendance.CheckInTime >= lateThreshold)
                                {
                                    // After 6:15 AM - Late arrival
                                    lateArrivalsCount++;
                                }
                            }
                        }
                        
                        // Calculate absent count - employees who haven't checked in
                        // We need to check if the current time is after the absent threshold
                        var currentSriLankaTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, sriLankaTimeZone).TimeOfDay;
                        
                        if (currentSriLankaTime >= absentThreshold)
                        {
                            absentCount = totalEmployees - totalArrivedCount;
                            if (absentCount < 0) absentCount = 0; // Ensure we don't have negative values
                        }
                    }
                    else
                    {
                        logger.LogWarning("Failed to retrieve attendance data: {Message}", 
                            attendanceResult.Message ?? "Unknown error");
                    }
                    
                    // Create and return the response
                    var dashboardStats = new
                    {
                        TotalEmployees = totalEmployees,
                        TotalArrived = totalArrivedCount,
                        OnTime = onTimeCount,
                        LateArrivals = lateArrivalsCount,
                        EarlyDeparture = earlyDepartureCount,
                        Absent = absentCount,
                        AsOf = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, sriLankaTimeZone)
                    };
                    
                    logger.LogInformation("Today's attendance statistics: Total={Total}, OnTime={OnTime}, Late={Late}, Early={Early}, Absent={Absent}",
                        dashboardStats.TotalArrived, dashboardStats.OnTime, dashboardStats.LateArrivals, 
                        dashboardStats.EarlyDeparture, dashboardStats.Absent);
                    
                    return Results.Ok(new ApiResponse<object>(true, "Today's attendance statistics retrieved successfully", dashboardStats));
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error retrieving today's attendance statistics");
                    return Results.BadRequest(new ApiResponse<object>(false, $"Error retrieving attendance statistics: {ex.Message}"));
                }
            })
            .WithName("GetTodayAttendanceStats")
            .WithDescription("Gets today's attendance statistics for the dashboard")
            .WithOpenApi();
        }
    }
}

//{
//   "attendanceDate": "2023-07-25",
//   "checkInTime": "09:15:00",
//   "checkOutTime": null
// }
