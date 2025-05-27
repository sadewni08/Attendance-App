using ChronoTrack.DTOs.Common;
using ChronoTrack.DTOs;

namespace ChronoTrack.Services.Interfaces
{
    public interface IReportService
    {
        // Export employee details
        Task<ApiResponse<byte[]>> ExportEmployeeDetailsAsync(DateTime? fromDate = null, DateTime? toDate = null, Guid? departmentId = null);

        // Export attendance list with filtering
        Task<ApiResponse<byte[]>> ExportAttendanceReportAsync(DateTime fromDate, DateTime toDate, Guid? departmentId = null, Guid? userId = null);

        // Get all employees attendance within a date range with pagination
        Task<ApiResponse<PagedResponse<AttendanceDto>>> GetAllEmployeesAttendanceAsync(DateTime fromDate, DateTime toDate, int page = 1, int pageSize = 10);

        // Get attendance summary for all employees within a date range
        Task<ApiResponse<AttendanceSummaryDto>> GetAttendanceSummaryAsync(DateTime fromDate, DateTime toDate, Guid? departmentId = null);
    }
}
