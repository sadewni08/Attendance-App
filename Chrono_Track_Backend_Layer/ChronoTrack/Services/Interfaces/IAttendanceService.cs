using ChronoTrack.DTOs.Common;
using ChronoTrack.DTOs;

namespace ChronoTrack.Services.Interfaces
{
    public interface IAttendanceService
    {
        Task<ApiResponse<AttendanceDto>> CheckInAsync(string userId, CreateAttendanceDto command);
        Task<ApiResponse<AttendanceDto>> CheckOutAsync(string userId, string attendanceId, UpdateAttendanceDto command);
        Task<ApiResponse<AttendanceDto>> GetAttendanceByIdAsync(Guid id);
        Task<ApiResponse<PagedResponse<AttendanceDto>>> GetUserAttendanceHistoryAsync(string userId, DateTime? fromDate = null, DateTime? toDate = null, int page = 1, int pageSize = 10);
        Task<ApiResponse<AttendanceSummaryDto>> GetDailyAttendanceSummaryAsync(DateTime date);
        Task<ApiResponse<UserAttendanceReportDto>> GetUserAttendanceReportAsync(string userId, DateTime fromDate, DateTime toDate);
        Task<ApiResponse<PagedResponse<AttendanceDto>>> GetDepartmentAttendanceAsync(Guid departmentId, DateTime date, int page = 1, int pageSize = 10);
        Task<ApiResponse<bool>> DeleteAttendanceAsync(Guid id);
        Task<ApiResponse<AttendanceDto>> UpdateAttendanceAsync(Guid id, AttendanceDto command);
        Task<ApiResponse<AttendanceStatusDto>> GetUserAttendanceStatusAsync(string userId);
        
        // New method specifically for check-in/check-out status
        Task<ApiResponse<CheckInCheckOutStatusDto>> CheckInCheckOutStatusAsync(string userId);

        // New method to get all employees' attendance for a date range
        Task<ApiResponse<PagedResponse<AttendanceDto>>> GetAllAttendanceByDateRangeAsync(DateTime fromDate, DateTime toDate, int page = 1, int pageSize = 10);
        
        // New method to get detailed attendance with various filtering options
        Task<ApiResponse<PagedResponse<DetailedAttendanceDto>>> GetDetailedAttendanceAsync(string userId = null, string attendanceId = null, string employeeName = null, DateTime? startDate = null, DateTime? endDate = null, int page = 1, int pageSize = 10);
    }
}
