using ChronoTrack.DTOs;
using ChronoTrack.DTOs.Common;

namespace ChronoTrack.Services.Interfaces
{
    public interface IUserRoleService
    {
        Task<ApiResponse<UserRoleDto>> CreateUserRoleAsync(CreateUserRoleDto command);
        Task<ApiResponse<UserRoleDto>> GetUserRoleByIdAsync(Guid id);
        Task<ApiResponse<PagedResponse<UserRoleDto>>> GetAllUserRolesAsync(int page = 1, int pageSize = 10);
        Task<ApiResponse<UserRoleDto>> UpdateUserRoleAsync(Guid id, UpdateUserRoleDto command);
        Task<ApiResponse<bool>> DeleteUserRoleAsync(Guid id);
        Task<ApiResponse<List<UserRoleDto>>> GetActiveUserRolesAsync();
    }
}