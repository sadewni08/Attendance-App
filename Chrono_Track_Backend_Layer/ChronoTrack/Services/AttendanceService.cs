using ChronoTrack.DTOs.Common;
using ChronoTrack.DTOs;
using ChronoTrack.Services.Interfaces;
using ChronoTrack.Persistence;
using System.Linq;
using Microsoft.EntityFrameworkCore;
namespace ChronoTrack.Services
{
        public class AttendanceService : IAttendanceService
        {
            private readonly ChronoTrackDbContext _dbContext;
            private readonly ILogger<AttendanceService> _logger;

            public AttendanceService(ChronoTrackDbContext dbContext, ILogger<AttendanceService> logger)
            {
                _dbContext = dbContext;
                _logger = logger;
            }

            public async Task<ApiResponse<AttendanceDto>> CheckInAsync(string userId, CreateAttendanceDto command)
            {
                try
                {
                    var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.UserId == userId);
                    if (user == null)
                        return new ApiResponse<AttendanceDto>(false, "User not found");

                    // Validate the attendance date is not in the future
                    if (command.AttendanceDate.Date > DateTime.UtcNow.Date)
                    {
                        _logger.LogWarning("User {UserId} attempted to check in for a future date: {Date}", userId, command.AttendanceDate.Date);
                        return new ApiResponse<AttendanceDto>(false, "Cannot check in for a future date.");
                    }

                    // Check if already checked in for today
                    var existingAttendance = await _dbContext.Attendances
                        .FirstOrDefaultAsync(a => a.UserID == user.Id && a.AttendanceDate.Date == command.AttendanceDate.Date);

                    if (existingAttendance != null)
                    {
                        _logger.LogWarning("User {UserId} attempted to check in multiple times on {Date}", userId, command.AttendanceDate.Date);
                        return new ApiResponse<AttendanceDto>(false, "You have already checked in for today. Only one check-in is allowed per day.");
                    }

                    // Validate check-in time is not in the future
                    var currentTime = DateTime.UtcNow.TimeOfDay;
                    if (command.CheckInTime > currentTime)
                    {
                        _logger.LogWarning("User {UserId} attempted to check in with a future time: {Time}", userId, command.CheckInTime);
                        return new ApiResponse<AttendanceDto>(false, "Check-in time cannot be in the future.");
                    }

                    var attendance = new Models.Attendance
                    {
                        UserID = user.Id,
                        AttendanceDate = command.AttendanceDate.Date,
                        CheckInTime = command.CheckInTime,
                        CheckOutTime = command.CheckOutTime ?? TimeSpan.Zero
                    };

                    await _dbContext.Attendances.AddAsync(attendance);
                    await _dbContext.SaveChangesAsync();

                    return new ApiResponse<AttendanceDto>(true, "Check-in successful",
                        new AttendanceDto(
                            attendance.Id,
                            user.UserId,
                            $"{user.FirstName} {user.LastName}",
                            attendance.AttendanceDate,
                            attendance.CheckInTime,
                            attendance.CheckOutTime,
                            attendance.CheckOutTime - attendance.CheckInTime
                        ));
                }
                catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("IX_Attendances_UserID_AttendanceDate") == true)
                {
                    _logger.LogWarning(ex, "Duplicate check-in attempt for user {UserId} on {Date}", userId, command.AttendanceDate.Date);
                    return new ApiResponse<AttendanceDto>(false, "You have already checked in for today. Only one check-in is allowed per day.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during check-in for user {UserId}", userId);
                    return new ApiResponse<AttendanceDto>(false, "Error during check-in");
                }
            }

            public async Task<ApiResponse<AttendanceDto>> CheckOutAsync(string userId, string attendanceId, UpdateAttendanceDto command)
            {
                try
                {
                    var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.UserId == userId);
                    if (user == null)
                        return new ApiResponse<AttendanceDto>(false, "User not found");

                    // Validate the attendance date is not in the future
                    if (command.AttendanceDate.Date > DateTime.UtcNow.Date)
                    {
                        _logger.LogWarning("User {UserId} attempted to check out for a future date: {Date}", userId, command.AttendanceDate.Date);
                        return new ApiResponse<AttendanceDto>(false, "Cannot check out for a future date.");
                    }

                    // Find attendance record by ID
                    Guid attendanceGuid;
                    if (!Guid.TryParse(attendanceId, out attendanceGuid))
                    {
                        _logger.LogWarning("Invalid attendance ID format: {AttendanceId}", attendanceId);
                        return new ApiResponse<AttendanceDto>(false, "Invalid attendance ID format.");
                    }

                    var attendance = await _dbContext.Attendances
                        .FirstOrDefaultAsync(a => a.Id == attendanceGuid && a.UserID == user.Id);

                    if (attendance == null)
                    {
                        _logger.LogWarning("Attendance record not found for ID: {AttendanceId} and user: {UserId}", attendanceId, userId);
                        return new ApiResponse<AttendanceDto>(false, "Attendance record not found.");
                    }

                    // Check if already checked out
                    if (attendance.CheckOutTime != TimeSpan.Zero)
                    {
                        _logger.LogWarning("User {UserId} attempted to check out multiple times for attendance: {AttendanceId}", userId, attendanceId);
                        return new ApiResponse<AttendanceDto>(false, "You have already checked out for this attendance record.");
                    }

                    // Validate check-out time is not before check-in time
                    if (command.CheckOutTime < attendance.CheckInTime)
                    {
                        _logger.LogWarning("User {UserId} attempted to check out with a time before check-in: CheckOut={CheckOut}, CheckIn={CheckIn}", 
                            userId, command.CheckOutTime, attendance.CheckInTime);
                        return new ApiResponse<AttendanceDto>(false, "Check-out time cannot be before check-in time.");
                    }

                    // Validate check-out time is not in the future
                    var currentTime = DateTime.UtcNow.TimeOfDay;
                    if (command.CheckOutTime > currentTime && attendance.AttendanceDate.Date == DateTime.UtcNow.Date)
                    {
                        _logger.LogWarning("User {UserId} attempted to check out with a future time: {Time}", userId, command.CheckOutTime);
                        return new ApiResponse<AttendanceDto>(false, "Check-out time cannot be in the future.");
                    }

                    attendance.CheckOutTime = command.CheckOutTime;
                    await _dbContext.SaveChangesAsync();

                    return new ApiResponse<AttendanceDto>(true, "Check-out successful",
                        new AttendanceDto(
                            attendance.Id,
                            user.UserId,
                            $"{user.FirstName} {user.LastName}",
                            attendance.AttendanceDate,
                            attendance.CheckInTime,
                            attendance.CheckOutTime,
                            attendance.CheckOutTime - attendance.CheckInTime
                        ));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during check-out for user {UserId}, attendance ID: {AttendanceId}", userId, attendanceId);
                    return new ApiResponse<AttendanceDto>(false, "Error during check-out");
                }
            }

            public async Task<ApiResponse<PagedResponse<AttendanceDto>>> GetUserAttendanceHistoryAsync(
                string userId,
                DateTime? fromDate = null,
                DateTime? toDate = null,
                int page = 1,
                int pageSize = 10)
            {
                try
                {
                    var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.UserId == userId);
                    if (user == null)
                        return new ApiResponse<PagedResponse<AttendanceDto>>(false, "User not found");

                    // Ensure dates are in UTC
                    var utcFromDate = fromDate.HasValue && fromDate.Value.Kind != DateTimeKind.Utc
                        ? DateTime.SpecifyKind(fromDate.Value.Date, DateTimeKind.Utc)
                        : fromDate;

                    var utcToDate = toDate.HasValue && toDate.Value.Kind != DateTimeKind.Utc
                        ? DateTime.SpecifyKind(toDate.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc)
                        : toDate.HasValue ? toDate.Value.Date.AddDays(1).AddTicks(-1) : toDate;

                    var query = _dbContext.Attendances
                        .Include(a => a.User)
                        .Where(a => a.UserID == user.Id);

                    if (utcFromDate.HasValue)
                    {
                        query = query.Where(a => a.AttendanceDate >= utcFromDate.Value.Date);
                    }

                    if (utcToDate.HasValue)
                    {
                        query = query.Where(a => a.AttendanceDate <= utcToDate.Value.Date);
                    }

                    var totalItems = await query.CountAsync();
                    var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

                    var attendances = await query
                        .OrderByDescending(a => a.AttendanceDate)
                        .Skip((page - 1) * pageSize)
                        .Take(pageSize)
                        .Select(a => new AttendanceDto(
                            a.Id,
                            a.User!.UserId,
                            $"{a.User.FirstName} {a.User.LastName}",
                            a.AttendanceDate,
                            a.CheckInTime,
                            a.CheckOutTime,
                            a.CheckOutTime - a.CheckInTime
                        ))
                        .ToListAsync();

                    var pagedResponse = new PagedResponse<AttendanceDto>(
                        attendances,
                        page,
                        pageSize,
                        totalItems,
                        totalPages
                    );

                    return new ApiResponse<PagedResponse<AttendanceDto>>(true, "Attendance history retrieved successfully", pagedResponse);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error retrieving attendance history: {Message}", ex.Message);
                    return new ApiResponse<PagedResponse<AttendanceDto>>(false, $"Error retrieving attendance history: {ex.Message}");
                }
            }

            public async Task<ApiResponse<AttendanceDto>> GetAttendanceByIdAsync(Guid id)
            {
                try
                {
                    var attendance = await _dbContext.Attendances
                        .Include(a => a.User)
                        .FirstOrDefaultAsync(a => a.Id == id);

                    if (attendance == null)
                        return new ApiResponse<AttendanceDto>(false, "Attendance record not found");

                    return new ApiResponse<AttendanceDto>(true, "Attendance record retrieved successfully",
                        new AttendanceDto(
                            attendance.Id,
                            attendance.User!.UserId,
                            $"{attendance.User.FirstName} {attendance.User.LastName}",
                            attendance.AttendanceDate,
                            attendance.CheckInTime,
                            attendance.CheckOutTime,
                            attendance.CheckOutTime - attendance.CheckInTime
                        ));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error retrieving attendance record");
                    return new ApiResponse<AttendanceDto>(false, "Error retrieving attendance record");
                }
            }

            public async Task<ApiResponse<AttendanceSummaryDto>> GetDailyAttendanceSummaryAsync(DateTime date)
            {
                try
                {
                    // Ensure date is in UTC
                    var utcDate = date.Kind != DateTimeKind.Utc ? DateTime.SpecifyKind(date.Date, DateTimeKind.Utc) : date.Date;

                    var totalEmployees = await _dbContext.Users.CountAsync();
                    var attendanceRecords = await _dbContext.Attendances
                        .Where(a => a.AttendanceDate.Date == utcDate.Date)
                        .ToListAsync();

                    var presentEmployees = attendanceRecords.Count;
                    var lateArrivals = attendanceRecords.Count(a => a.CheckInTime.Hours >= 9 && a.CheckInTime.Minutes > 0);
                    var averageWorkingHours = attendanceRecords.Any()
                        ? attendanceRecords
                            .Where(a => a.CheckOutTime != TimeSpan.Zero)
                            .Average(a => (a.CheckOutTime - a.CheckInTime).TotalHours)
                        : 0;

                    var summary = new AttendanceSummaryDto(
                        utcDate,
                        totalEmployees,
                        presentEmployees,
                        lateArrivals,
                        averageWorkingHours
                    );

                    return new ApiResponse<AttendanceSummaryDto>(true, "Daily attendance summary retrieved successfully", summary);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error retrieving daily attendance summary");
                    return new ApiResponse<AttendanceSummaryDto>(false, "Error retrieving daily attendance summary");
                }
            }

            public async Task<ApiResponse<UserAttendanceReportDto>> GetUserAttendanceReportAsync(string userId, DateTime fromDate, DateTime toDate)
            {
                try
                {
                    // Ensure dates are in UTC
                    var utcFromDate = fromDate.Kind != DateTimeKind.Utc ? DateTime.SpecifyKind(fromDate.Date, DateTimeKind.Utc) : fromDate.Date;
                    var utcToDate = toDate.Kind != DateTimeKind.Utc ? DateTime.SpecifyKind(toDate.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc) : toDate.Date.AddDays(1).AddTicks(-1);

                    var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.UserId == userId);
                    if (user == null)
                    {
                        return new ApiResponse<UserAttendanceReportDto>(false, "User not found");
                    }

                    var attendanceRecords = await _dbContext.Attendances
                        .Where(a => a.UserID == user.Id && a.AttendanceDate >= utcFromDate && a.AttendanceDate <= utcToDate)
                        .ToListAsync();

                    var totalDays = (toDate - fromDate).Days + 1;
                    var presentDays = attendanceRecords.Count;
                    var lateDays = attendanceRecords.Count(a => a.CheckInTime.Hours >= 9 && a.CheckInTime.Minutes > 0);
                    var averageWorkingHours = attendanceRecords.Any()
                        ? attendanceRecords
                            .Where(a => a.CheckOutTime != TimeSpan.Zero)
                            .Average(a => (a.CheckOutTime - a.CheckInTime).TotalHours)
                        : 0;

                    var attendanceDetails = attendanceRecords.Select(a => new AttendanceDto(
                        a.Id,
                        user.UserId,
                        $"{user.FirstName} {user.LastName}",
                        a.AttendanceDate,
                        a.CheckInTime,
                        a.CheckOutTime,
                        a.CheckOutTime - a.CheckInTime
                    )).ToList();

                    var report = new UserAttendanceReportDto(
                        user.UserId,
                        $"{user.FirstName} {user.LastName}",
                        user.Department!.DepartmentName,
                        totalDays,
                        presentDays,
                        lateDays,
                        averageWorkingHours,
                        attendanceDetails
                    );

                    return new ApiResponse<UserAttendanceReportDto>(true, "User attendance report retrieved successfully", report);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error retrieving user attendance report: {Message}", ex.Message);
                    return new ApiResponse<UserAttendanceReportDto>(false, $"Error retrieving user attendance report: {ex.Message}");
                }
            }

            public async Task<ApiResponse<PagedResponse<AttendanceDto>>> GetDepartmentAttendanceAsync(Guid departmentId, DateTime date, int page = 1, int pageSize = 10)
            {
                try
                {
                    // Ensure date is in UTC
                    var utcDate = date.Kind != DateTimeKind.Utc ? DateTime.SpecifyKind(date.Date, DateTimeKind.Utc) : date.Date;

                    var departmentUsers = await _dbContext.Users
                        .Where(u => u.DepartmentID == departmentId)
                        .Select(u => u.Id)
                        .ToListAsync();

                    if (!departmentUsers.Any())
                    {
                        return new ApiResponse<PagedResponse<AttendanceDto>>(false, "No users found in the department");
                    }

                    var query = _dbContext.Attendances
                        .Include(a => a.User)
                        .Where(a => departmentUsers.Contains(a.UserID) && a.AttendanceDate.Date == utcDate.Date);

                    var totalItems = await query.CountAsync();
                    var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

                    var attendances = await query
                        .OrderBy(a => a.CheckInTime)
                        .Skip((page - 1) * pageSize)
                        .Take(pageSize)
                        .Select(a => new AttendanceDto(
                            a.Id,
                            a.User!.UserId,
                            $"{a.User.FirstName} {a.User.LastName}",
                            a.AttendanceDate,
                            a.CheckInTime,
                            a.CheckOutTime,
                            a.CheckOutTime - a.CheckInTime
                        ))
                        .ToListAsync();

                    var pagedResponse = new PagedResponse<AttendanceDto>(
                        attendances,
                        page,
                        pageSize,
                        totalItems,
                        totalPages
                    );

                    return new ApiResponse<PagedResponse<AttendanceDto>>(true, "Department attendance retrieved successfully", pagedResponse);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error retrieving department attendance");
                    return new ApiResponse<PagedResponse<AttendanceDto>>(false, "Error retrieving department attendance");
                }
            }

            public async Task<ApiResponse<bool>> DeleteAttendanceAsync(Guid id)
            {
                try
                {
                    var attendance = await _dbContext.Attendances.FindAsync(id);
                    if (attendance == null)
                        return new ApiResponse<bool>(false, "Attendance record not found");

                    _dbContext.Attendances.Remove(attendance);
                    await _dbContext.SaveChangesAsync();

                    return new ApiResponse<bool>(true, "Attendance record deleted successfully", true);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error deleting attendance record");
                    return new ApiResponse<bool>(false, "Error deleting attendance record");
                }
            }

            public async Task<ApiResponse<AttendanceDto>> UpdateAttendanceAsync(Guid id, AttendanceDto command)
            {
                try
                {
                    var attendance = await _dbContext.Attendances
                        .Include(a => a.User)
                        .FirstOrDefaultAsync(a => a.Id == id);

                    if (attendance == null)
                        return new ApiResponse<AttendanceDto>(false, "Attendance record not found");

                    attendance.CheckInTime = command.CheckInTime;
                    attendance.CheckOutTime = command.CheckOutTime;
                    await _dbContext.SaveChangesAsync();

                    return new ApiResponse<AttendanceDto>(true, "Attendance record updated successfully",
                        new AttendanceDto(
                            attendance.Id,
                            attendance.User!.UserId,
                            $"{attendance.User.FirstName} {attendance.User.LastName}",
                            attendance.AttendanceDate,
                            attendance.CheckInTime,
                            attendance.CheckOutTime,
                            attendance.CheckOutTime - attendance.CheckInTime
                        ));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating attendance record");
                    return new ApiResponse<AttendanceDto>(false, "Error updating attendance record");
                }
            }

            public async Task<ApiResponse<PagedResponse<AttendanceDto>>> GetAllAttendanceByDateRangeAsync(DateTime fromDate, DateTime toDate, int page = 1, int pageSize = 10)
            {
                try
                {
                    _logger.LogInformation($"Getting all attendance records from {fromDate:yyyy-MM-dd} to {toDate:yyyy-MM-dd}");

                    var query = _dbContext.Attendances
                        .Include(a => a.User)
                        .Where(a => a.AttendanceDate.Date >= fromDate.Date && a.AttendanceDate.Date <= toDate.Date)
                        .OrderByDescending(a => a.AttendanceDate)
                        .ThenBy(a => a.User.FirstName);

                    var totalCount = await query.CountAsync();
                    var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

                    var attendances = await query
                        .Skip((page - 1) * pageSize)
                        .Take(pageSize)
                        .Select(a => new AttendanceDto(
                            a.Id,
                            a.User.UserId,
                            $"{a.User.FirstName} {a.User.LastName}",
                            a.AttendanceDate,
                            a.CheckInTime,
                            a.CheckOutTime,
                            a.CheckOutTime == TimeSpan.Zero ? TimeSpan.Zero : a.CheckOutTime - a.CheckInTime
                        ))
                        .ToListAsync();

                    var pagedResponse = new PagedResponse<AttendanceDto>(
                        attendances,
                        page,
                        pageSize,
                        totalCount,
                        totalPages
                    );

                    return new ApiResponse<PagedResponse<AttendanceDto>>(true, "Attendance records retrieved successfully", pagedResponse);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error retrieving all attendance records for date range");
                    return new ApiResponse<PagedResponse<AttendanceDto>>(false, $"Error retrieving attendance records: {ex.Message}");
                }
            }

            public async Task<ApiResponse<AttendanceDto>> GetAttendanceByUserIdAsync(string userId)
            {
                try
                {
                    var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.UserId == userId);
                    if (user == null)
                        return new ApiResponse<AttendanceDto>(false, "User not found");

                    var attendance = await _dbContext.Attendances
                        .Include(a => a.User)
                        .FirstOrDefaultAsync(a => a.UserID == user.Id);

                    if (attendance == null)
                        return new ApiResponse<AttendanceDto>(false, "Attendance record not found");

                    return new ApiResponse<AttendanceDto>(true, "Attendance record retrieved successfully",
                        new AttendanceDto(
                            attendance.Id,
                            attendance.User!.UserId,
                            $"{attendance.User.FirstName} {attendance.User.LastName}",
                            attendance.AttendanceDate,
                            attendance.CheckInTime,
                            attendance.CheckOutTime,
                            attendance.CheckOutTime - attendance.CheckInTime
                        ));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error retrieving attendance record: {Message}", ex.Message);
                    return new ApiResponse<AttendanceDto>(false, $"Error retrieving attendance record: {ex.Message}");
                }
            }

            public async Task<ApiResponse<PagedResponse<DetailedAttendanceDto>>> GetDetailedAttendanceAsync(
                string userId = null, 
                string attendanceId = null, 
                string employeeName = null, 
                DateTime? startDate = null, 
                DateTime? endDate = null, 
                int page = 1, 
                int pageSize = 10)
            {
                try
                {
                    _logger.LogInformation("Getting detailed attendance with filters: UserId={UserId}, AttendanceId={AttendanceId}, EmployeeName={EmployeeName}, StartDate={StartDate}, EndDate={EndDate}",
                        userId, attendanceId, employeeName, startDate, endDate);
                    
                    // Use a simple approach to avoid SQL translation issues
                    // First, get the base attendance records
                    var query = _dbContext.Attendances.AsQueryable();
                    
                    // Apply filters directly on attendance records first
                    if (!string.IsNullOrEmpty(attendanceId) && Guid.TryParse(attendanceId, out Guid attendanceGuid))
                    {
                        query = query.Where(a => a.Id == attendanceGuid);
                    }
                    
                    if (startDate.HasValue)
                    {
                        var utcStartDate = startDate.Value.Kind != DateTimeKind.Utc 
                            ? DateTime.SpecifyKind(startDate.Value.Date, DateTimeKind.Utc)
                            : startDate.Value;
                        
                        query = query.Where(a => a.AttendanceDate >= utcStartDate.Date);
                    }
                    
                    if (endDate.HasValue)
                    {
                        var utcEndDate = endDate.Value.Kind != DateTimeKind.Utc
                            ? DateTime.SpecifyKind(endDate.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc)
                            : endDate.Value.Date.AddDays(1).AddTicks(-1);
                        
                        query = query.Where(a => a.AttendanceDate <= utcEndDate.Date);
                    }
                    
                    // Get user IDs for the query
                    var userQuery = _dbContext.Users.AsQueryable();
                    
                    if (!string.IsNullOrEmpty(userId))
                    {
                        userQuery = userQuery.Where(u => u.UserId == userId);
                    }
                    
                    if (!string.IsNullOrEmpty(employeeName))
                    {
                        employeeName = employeeName.ToLower();
                        userQuery = userQuery.Where(u => 
                            (u.FirstName + " " + u.LastName).ToLower().Contains(employeeName) || 
                            u.FirstName.ToLower().Contains(employeeName) || 
                            u.LastName.ToLower().Contains(employeeName));
                    }
                    
                    var filteredUserIds = await userQuery.Select(u => u.Id).ToListAsync();
                    
                    // Apply user filter if any user criteria were specified
                    if (!string.IsNullOrEmpty(userId) || !string.IsNullOrEmpty(employeeName))
                    {
                        query = query.Where(a => filteredUserIds.Contains(a.UserID));
                    }
                    
                    // Count total items for pagination
                    var totalItems = await query.CountAsync();
                    var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
                    
                    // Get paginated attendance records
                    var attendances = await query
                        .OrderByDescending(a => a.AttendanceDate)
                        .ThenBy(a => a.User.LastName)
                        .ThenBy(a => a.User.FirstName)
                        .Skip((page - 1) * pageSize)
                        .Take(pageSize)
                        .ToListAsync();
                    
                    // Get all user IDs for these attendance records
                    var attendanceUserIds = attendances.Select(a => a.UserID).Distinct().ToList();
                    
                    // Get user details in a separate query to avoid complex joins in SQL
                    var users = await _dbContext.Users
                        .Where(u => attendanceUserIds.Contains(u.Id))
                        .Include(u => u.Department)
                        .Include(u => u.UserRole)
                        .ToDictionaryAsync(u => u.Id);
                    
                    // Build DTOs manually
                    var detailedDtos = new List<DetailedAttendanceDto>();
                    
                    foreach (var attendance in attendances)
                    {
                        if (users.TryGetValue(attendance.UserID, out var user))
                        {
                            string status = DetermineStatus(attendance.CheckInTime, attendance.CheckOutTime);
                            string department = "Not Assigned";
                            string role = "Not Assigned";
                            
                            // Safely access Department and UserRole
                            if (user.Department != null)
                            {
                                department = user.Department.DepartmentName;
                            }
                            
                            if (user.UserRole != null)
                            {
                                role = user.UserRole.UserRoleName;
                            }
                            
                            detailedDtos.Add(new DetailedAttendanceDto(
                                attendance.Id,
                                user.UserId,
                                $"{user.FirstName} {user.LastName}",
                                department,
                                role,
                                attendance.AttendanceDate,
                                attendance.CheckInTime,
                                attendance.CheckOutTime,
                                attendance.CheckOutTime != TimeSpan.Zero ? attendance.CheckOutTime - attendance.CheckInTime : TimeSpan.Zero,
                                status
                            ));
                        }
                    }
                    
                    var pagedResponse = new PagedResponse<DetailedAttendanceDto>(
                        detailedDtos,
                        page,
                        pageSize,
                        totalItems,
                        totalPages
                    );
                    
                    return new ApiResponse<PagedResponse<DetailedAttendanceDto>>(
                        true, 
                        $"Retrieved {detailedDtos.Count} attendance records successfully", 
                        pagedResponse);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error retrieving detailed attendance: {ErrorMessage}. Stack trace: {StackTrace}", ex.Message, ex.StackTrace);
                    return new ApiResponse<PagedResponse<DetailedAttendanceDto>>(false, $"Error retrieving detailed attendance: {ex.Message}");
                }
            }
            
            private string DetermineStatus(TimeSpan checkInTime, TimeSpan checkOutTime)
            {
                // Define the expected check-in time (e.g., 9:00 AM)
                var expectedCheckInTime = new TimeSpan(9, 0, 0);
                
                if (checkOutTime == TimeSpan.Zero)
                {
                    return "Active";
                }
                else if (checkInTime > expectedCheckInTime)
                {
                    return "Late";
                }
                else
                {
                    return "Present";
                }
            }

            public async Task<ApiResponse<AttendanceStatusDto>> GetUserAttendanceStatusAsync(string userId)
            {
                try
                {
                    var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.UserId == userId);
                    if (user == null)
                        return new ApiResponse<AttendanceStatusDto>(false, "User not found");

                    // Check if there's attendance for today
                    var todayAttendance = await _dbContext.Attendances
                        .FirstOrDefaultAsync(a => a.UserID == user.Id && a.AttendanceDate.Date == DateTime.UtcNow.Date);

                    if (todayAttendance == null)
                    {
                        return new ApiResponse<AttendanceStatusDto>(true, "User is not checked in today",
                            new AttendanceStatusDto
                            {
                                IsCheckedIn = false,
                                CheckedOutToday = false
                            });
                    }

                    bool isCheckedOut = todayAttendance.CheckOutTime != TimeSpan.Zero;

                    return new ApiResponse<AttendanceStatusDto>(true, isCheckedOut ? "User has checked out today" : "User is checked in",
                        new AttendanceStatusDto
                        {
                            IsCheckedIn = true,
                            CheckedOutToday = isCheckedOut,
                            AttendanceId = todayAttendance.Id.ToString()
                        });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error retrieving attendance status for user: {UserId}", userId);
                    return new ApiResponse<AttendanceStatusDto>(false, $"Error retrieving attendance status: {ex.Message}");
                }
            }

            public async Task<ApiResponse<CheckInCheckOutStatusDto>> CheckInCheckOutStatusAsync(string userId)
            {
                try
                {
                    _logger.LogInformation("Getting check-in/check-out status for user: {UserId}", userId);
                    
                    var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.UserId == userId);
                    if (user == null)
                    {
                        _logger.LogWarning("User not found: {UserId}", userId);
                        return new ApiResponse<CheckInCheckOutStatusDto>(false, "User not found");
                    }

                    // Get today's attendance record if it exists
                    var todayAttendance = await _dbContext.Attendances
                        .FirstOrDefaultAsync(a => a.UserID == user.Id && a.AttendanceDate.Date == DateTime.UtcNow.Date);

                    if (todayAttendance == null)
                    {
                        _logger.LogInformation("No check-in record found today for user: {UserId}", userId);
                        return new ApiResponse<CheckInCheckOutStatusDto>(true, "User has not checked in today", 
                            new CheckInCheckOutStatusDto(
                                userId,
                                $"{user.FirstName} {user.LastName}",
                                false,
                                string.Empty,
                                null,
                                null,
                                null,
                                null
                            ));
                    }

                    bool isCheckedIn = true;
                    bool hasCheckedOut = todayAttendance.CheckOutTime != TimeSpan.Zero;
                    TimeSpan? duration = hasCheckedOut ? todayAttendance.CheckOutTime - todayAttendance.CheckInTime : null;
                    
                    _logger.LogInformation("User {UserId} status: IsCheckedIn={IsCheckedIn}, HasCheckedOut={HasCheckedOut}", 
                        userId, isCheckedIn, hasCheckedOut);

                    return new ApiResponse<CheckInCheckOutStatusDto>(true, hasCheckedOut ? "User has checked out" : "User is checked in",
                        new CheckInCheckOutStatusDto(
                            userId,
                            $"{user.FirstName} {user.LastName}",
                            isCheckedIn,
                            todayAttendance.Id.ToString(),
                            todayAttendance.AttendanceDate,
                            todayAttendance.CheckInTime,
                            hasCheckedOut ? todayAttendance.CheckOutTime : null,
                            duration
                        ));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error retrieving check-in/check-out status for user: {UserId}", userId);
                    return new ApiResponse<CheckInCheckOutStatusDto>(false, $"Error retrieving status: {ex.Message}");
                }
            }

            // Other interface implementations...
        }
    
}
