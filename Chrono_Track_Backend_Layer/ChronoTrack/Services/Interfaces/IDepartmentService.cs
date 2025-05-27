using ChronoTrack.DTOs.Common;
using ChronoTrack.DTOs;

namespace ChronoTrack.Services.Interfaces
{
    public interface IDepartmentService
    {
        Task<ApiResponse<DepartmentDto>> CreateDepartmentAsync(CreateDepartmentDto command);
        Task<ApiResponse<DepartmentDto>> GetDepartmentByIdAsync(Guid id);
        Task<ApiResponse<PagedResponse<DepartmentDto>>> GetAllDepartmentsAsync(int page = 1, int pageSize = 10);
        Task<ApiResponse<DepartmentDto>> UpdateDepartmentAsync(Guid id, UpdateDepartmentDto command);
        Task<ApiResponse<bool>> DeleteDepartmentAsync(Guid id);
        Task<ApiResponse<List<DepartmentDto>>> GetActiveDepartmentsAsync();
        Task<ApiResponse<DepartmentSummaryDto>> GetDepartmentSummaryAsync(Guid id, DateTime? fromDate = null, DateTime? toDate = null);
        Task<ApiResponse<List<DepartmentSummaryDto>>> GetAllDepartmentsSummaryAsync(DateTime? fromDate = null, DateTime? toDate = null);
    }
}
