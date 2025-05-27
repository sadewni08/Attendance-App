using ChronoTrack.DTOs.Common;
using ChronoTrack.DTOs;

namespace ChronoTrack.Services.Interfaces
{
    public interface IUserService
    {
        Task<ApiResponse<UserDto>> CreateUserAsync(CreateUserDto command);
        Task<ApiResponse<UserDto>> GetUserByIdAsync(Guid id);
        Task<ApiResponse<UserDto>> GetUserByUserIdAsync(string userId);
        Task<ApiResponse<PagedResponse<UserSummaryDto>>> GetAllUsersAsync(int page = 1, int pageSize = 10);
        Task<ApiResponse<UserDto>> UpdateUserAsync(Guid id, UpdateUserDto command);
        Task<ApiResponse<UserDto>> UpdateUserByUserIdAsync(string userId, UpdateUserDto command);
        Task<ApiResponse<bool>> DeleteUserAsync(Guid id);
        Task<ApiResponse<bool>> DeleteUserByUserIdAsync(string userId);
        Task<ApiResponse<UserAuthResponseDto>> AuthenticateAsync(UserLoginDto loginDto);
        Task<ApiResponse<PagedResponse<UserDto>>> GetUsersByDepartmentAsync(Guid departmentId, int page = 1, int pageSize = 10);
        Task<ApiResponse<PagedResponse<UserDto>>> GetUsersByRoleAsync(Guid roleId, int page = 1, int pageSize = 10);
        Task<ApiResponse<bool>> IsUserRegisteredByEmailAsync(string email);
        Task<ApiResponse<UserAuthResponseDto>> AuthenticateByEmailAsync(string email);
    }
}
