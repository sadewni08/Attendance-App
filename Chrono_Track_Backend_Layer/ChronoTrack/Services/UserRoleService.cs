using ChronoTrack.DTOs;
using ChronoTrack.DTOs.Common;
using ChronoTrack.Persistence;
using ChronoTrack.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ChronoTrack.Services
{
    public class UserRoleService : IUserRoleService
    {
        private readonly ChronoTrackDbContext _dbContext;
        private readonly ILogger<UserRoleService> _logger;

        public UserRoleService(ChronoTrackDbContext dbContext, ILogger<UserRoleService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<ApiResponse<UserRoleDto>> CreateUserRoleAsync(CreateUserRoleDto command)
        {
            try
            {
                var userRole = new Models.UserRole
                {
                    UserRoleName = command.UserRoleName
                };

                await _dbContext.UserRoles.AddAsync(userRole);
                await _dbContext.SaveChangesAsync();

                var createdRole = await GetUserRoleDtoById(userRole.Id);
                return new ApiResponse<UserRoleDto>(true, "User role created successfully", createdRole);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user role");
                return new ApiResponse<UserRoleDto>(false, "Error creating user role");
            }
        }

        public async Task<ApiResponse<UserRoleDto>> GetUserRoleByIdAsync(Guid id)
        {
            try
            {
                var userRole = await GetUserRoleDtoById(id);
                if (userRole == null)
                    return new ApiResponse<UserRoleDto>(false, "User role not found");

                return new ApiResponse<UserRoleDto>(true, "User role retrieved successfully", userRole);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user role");
                return new ApiResponse<UserRoleDto>(false, "Error retrieving user role");
            }
        }

        public async Task<ApiResponse<PagedResponse<UserRoleDto>>> GetAllUserRolesAsync(int page = 1, int pageSize = 10)
        {
            try
            {
                var query = _dbContext.UserRoles
                    .OrderBy(ur => ur.UserRoleName)
                    .AsNoTracking();
                var totalItems = await query.CountAsync();
                var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

                var userRoles = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(ur => new UserRoleDto(
                        ur.Id,
                        ur.UserRoleName,
                        ur.Users.Count
                    ))
                    .ToListAsync();

                var pagedResponse = new PagedResponse<UserRoleDto>(
                    userRoles,
                    page,
                    pageSize,
                    totalItems,
                    totalPages
                );

                return new ApiResponse<PagedResponse<UserRoleDto>>(true, "User roles retrieved successfully", pagedResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user roles");
                return new ApiResponse<PagedResponse<UserRoleDto>>(false, "Error retrieving user roles");
            }
        }

        public async Task<ApiResponse<UserRoleDto>> UpdateUserRoleAsync(Guid id, UpdateUserRoleDto command)
        {
            try
            {
                var userRole = await _dbContext.UserRoles.FindAsync(id);
                if (userRole == null)
                    return new ApiResponse<UserRoleDto>(false, "User role not found");

                userRole.UserRoleName = command.UserRoleName;
                await _dbContext.SaveChangesAsync();

                var updatedRole = await GetUserRoleDtoById(userRole.Id);
                return new ApiResponse<UserRoleDto>(true, "User role updated successfully", updatedRole);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user role");
                return new ApiResponse<UserRoleDto>(false, "Error updating user role");
            }
        }

        public async Task<ApiResponse<bool>> DeleteUserRoleAsync(Guid id)
        {
            try
            {
                var userRole = await _dbContext.UserRoles.FindAsync(id);
                if (userRole == null)
                    return new ApiResponse<bool>(false, "User role not found");

                _dbContext.UserRoles.Remove(userRole);
                await _dbContext.SaveChangesAsync();

                return new ApiResponse<bool>(true, "User role deleted successfully", true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user role");
                return new ApiResponse<bool>(false, "Error deleting user role");
            }
        }

        public async Task<ApiResponse<List<UserRoleDto>>> GetActiveUserRolesAsync()
        {
            try
            {
                var userRoles = await _dbContext.UserRoles
                    .AsNoTracking()
                    .Select(ur => new UserRoleDto(
                        ur.Id,
                        ur.UserRoleName,
                        ur.Users.Count
                    ))
                    .ToListAsync();

                return new ApiResponse<List<UserRoleDto>>(true, "Active user roles retrieved successfully", userRoles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving active user roles");
                return new ApiResponse<List<UserRoleDto>>(false, "Error retrieving active user roles");
            }
        }

        private async Task<UserRoleDto?> GetUserRoleDtoById(Guid id)
        {
            return await _dbContext.UserRoles
                .Where(ur => ur.Id == id)
                .Select(ur => new UserRoleDto(
                    ur.Id,
                    ur.UserRoleName,
                    ur.Users.Count
                ))
                .FirstOrDefaultAsync();
        }
    }
} 