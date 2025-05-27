using ChronoTrack_ViewLayer.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ChronoTrack_ViewLayer.Services.Interfaces
{
    public interface IAttendanceService
    {
        Task<ServiceResponse<PagedResponse<AttendanceDto>>> GetUserAttendanceHistoryAsync(
            string userId, 
            DateTime? startDate = null, 
            DateTime? endDate = null,
            int page = 1,
            int pageSize = 10);
            
        Task<ServiceResponse<AttendanceDto>> CheckInAsync(string userId, CreateAttendanceDto command);
        Task<ServiceResponse<AttendanceDto>> CheckOutAsync(string userId, string attendanceId, UpdateAttendanceDto command);
        Task<ServiceResponse<bool>> AutoCheckoutUsersAsync();
        Task<ServiceResponse<PagedResponse<AttendanceDto>>> GetAllAttendanceByDateRangeAsync(DateTime fromDate, DateTime toDate, int page = 1, int pageSize = 50);
        
        // Add the missing method for getting attendance by user ID within a date range
        Task<ServiceResponse<List<AttendanceDto>>> GetAttendanceByUserIdAsync(string userId, DateTime startDate, DateTime endDate);
        
        // Add detailed attendance method
        Task<ServiceResponse<PagedResponse<DetailedAttendanceDto>>> GetDetailedAttendanceAsync(
            string userId = null, 
            string attendanceId = null, 
            string employeeName = null, 
            DateTime? startDate = null, 
            DateTime? endDate = null, 
            int page = 1, 
            int pageSize = 10);
            
        // Add method for checking check-in/check-out status
        Task<ServiceResponse<CheckInCheckOutStatusDto>> GetCheckInCheckOutStatusAsync(string userId);
        
        // New method to get today's attendance statistics
        Task<ServiceResponse<AttendanceStatsDto>> GetTodayAttendanceStatsAsync();
    }
} 