using ChronoTrack.DTOs.Common;
using ChronoTrack.DTOs;
using ChronoTrack.Persistence;
using Microsoft.EntityFrameworkCore;
using ChronoTrack.Services.Interfaces;

namespace ChronoTrack.Services
{
    public class UserService : IUserService
    {
        private readonly ChronoTrackDbContext _dbContext;
        private readonly ILogger<UserService> _logger;

        public UserService(ChronoTrackDbContext dbContext, ILogger<UserService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<ApiResponse<UserDto>> CreateUserAsync(CreateUserDto command)
        {
            try
            {
                // Log the command details for debugging
                _logger.LogInformation("Creating user with UserId: {UserId}, Name: {FirstName} {LastName}, Email: {Email}, DepartmentID: {DepartmentID}, UserRoleID: {UserRoleID}",
                    command.UserId, command.FirstName, command.LastName, command.EmailAddress, command.DepartmentID, command.UserRoleID);
                
                // Check if email already exists
                var emailExists = await _dbContext.Users
                    .AnyAsync(u => u.EmailAddress == command.EmailAddress);

                if (emailExists)
                {
                    _logger.LogWarning("Email already exists: {Email}", command.EmailAddress);
                    return new ApiResponse<UserDto>(false, "Email already exists");
                }
                
                // Check if required values are valid
                if (command.DepartmentID == Guid.Empty)
                {
                    _logger.LogWarning("Invalid DepartmentID: {DepartmentID}", command.DepartmentID);
                    return new ApiResponse<UserDto>(false, "Invalid Department ID");
                }
                
                if (command.UserRoleID == Guid.Empty)
                {
                    _logger.LogWarning("Invalid UserRoleID: {UserRoleID}", command.UserRoleID);
                    return new ApiResponse<UserDto>(false, "Invalid User Role ID");
                }
                
                // Check if department exists
                var departmentExists = await _dbContext.Departments.AnyAsync(d => d.Id == command.DepartmentID);
                if (!departmentExists)
                {
                    _logger.LogWarning("Department with ID {DepartmentID} doesn't exist", command.DepartmentID);
                    return new ApiResponse<UserDto>(false, $"Department with ID {command.DepartmentID} doesn't exist");
                }
                
                // Check if role exists
                var roleExists = await _dbContext.UserRoles.AnyAsync(r => r.Id == command.UserRoleID);
                if (!roleExists)
                {
                    _logger.LogWarning("User Role with ID {UserRoleID} doesn't exist", command.UserRoleID);
                    return new ApiResponse<UserDto>(false, $"User Role with ID {command.UserRoleID} doesn't exist");
                }

                var userTypeId = command.UserTypeID;
                
                // Only look up the UserTypeID if not provided by the client
                if (userTypeId == Guid.Empty)
                {
                    userTypeId = await _dbContext.UserTypes.Where(ut => ut.TypeName == Models.UserTypeEnum.User)
                        .Select(ut => ut.Id)
                        .FirstOrDefaultAsync();
                        
                    if (userTypeId == Guid.Empty)
                    {
                        _logger.LogError("User Type not found with name: {TypeName}", Models.UserTypeEnum.User);
                        return new ApiResponse<UserDto>(false, "User Type not found");
                    }
                }
                else
                {
                    // Verify the UserTypeID exists if provided
                    var userTypeExists = await _dbContext.UserTypes.AnyAsync(ut => ut.Id == userTypeId);
                    if (!userTypeExists)
                    {
                        _logger.LogWarning("User Type with ID {UserTypeID} doesn't exist", userTypeId);
                        return new ApiResponse<UserDto>(false, $"User Type with ID {userTypeId} doesn't exist");
                    }
                }

                var user = new Models.User
                {
                    UserId = command.UserId,
                    FirstName = command.FirstName,
                    LastName = command.LastName,
                    Gender = Enum.Parse<Models.GenderEnum>(command.Gender),
                    Address = command.Address,
                    EmailAddress = command.EmailAddress,
                    PhoneNumber = command.PhoneNumber,
                    Password = command.Password, // TODO: Hash password before saving
                    DepartmentID = command.DepartmentID,
                    UserRoleID = command.UserRoleID,
                    UserTypeID = userTypeId
                };

                await _dbContext.Users.AddAsync(user);
                
                _logger.LogInformation("Saving new user to database");
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("User saved successfully with ID: {UserId}", user.Id);

                var createdUser = await GetUserDtoById(user.Id);
                return new ApiResponse<UserDto>(true, "User created successfully", createdUser);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user: {Message}", ex.Message);
                if (ex.InnerException != null)
                {
                    _logger.LogError("Inner exception: {Message}", ex.InnerException.Message);
                }
                return new ApiResponse<UserDto>(false, $"Error creating user: {ex.Message}");
            }
        }

        public async Task<ApiResponse<UserDto>> GetUserByIdAsync(Guid id)
        {
            try
            {
                var user = await GetUserDtoById(id);
                if (user == null)
                    return new ApiResponse<UserDto>(false, "User not found");

                return new ApiResponse<UserDto>(true, "User retrieved successfully", user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user");
                return new ApiResponse<UserDto>(false, "Error retrieving user");
            }
        }

        public async Task<ApiResponse<UserDto>> GetUserByUserIdAsync(string userId)
        {
            try
            {
                var user = await _dbContext.Users
                    .Include(u => u.Department)
                    .Include(u => u.UserRole)
                    .Where(u => u.UserId == userId)
                    .Select(u => new UserDto(
                        u.Id,
                        u.UserId,
                        u.FirstName,
                        u.LastName,
                        u.Gender.ToString(),
                        u.Department!.DepartmentName,
                        u.UserRole!.UserRoleName,
                        u.Address,
                        u.EmailAddress,
                        u.PhoneNumber
                    ))
                    .FirstOrDefaultAsync();

                if (user == null)
                    return new ApiResponse<UserDto>(false, "User not found");

                return new ApiResponse<UserDto>(true, "User retrieved successfully", user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user");
                return new ApiResponse<UserDto>(false, "Error retrieving user");
            }
        }

        public async Task<ApiResponse<PagedResponse<UserSummaryDto>>> GetAllUsersAsync(int page = 1, int pageSize = 10)
        {
            try
            {
                var query = _dbContext.Users
                    .Include(u => u.Department)
                    .Include(u => u.UserRole)
                    .OrderBy(u => u.UserId)
                    .AsNoTracking();

                var totalItems = await query.CountAsync();
                var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

                var users = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(u => new UserSummaryDto(
                        u.UserId,
                        $"{u.FirstName} {u.LastName}",
                        u.Department!.DepartmentName,
                        u.UserRole!.UserRoleName,
                        u.Gender.ToString(),
                        u.EmailAddress,
                        u.PhoneNumber,
                        u.Address
                    ))
                    .ToListAsync();

                var pagedResponse = new PagedResponse<UserSummaryDto>(
                    users,
                    page,
                    pageSize,
                    totalItems,
                    totalPages
                );

                return new ApiResponse<PagedResponse<UserSummaryDto>>(true, "Users retrieved successfully", pagedResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving users");
                return new ApiResponse<PagedResponse<UserSummaryDto>>(false, "Error retrieving users");
            }
        }

        public async Task<ApiResponse<UserDto>> UpdateUserAsync(Guid id, UpdateUserDto command)
        {
            try
            {
                var user = await _dbContext.Users.FindAsync(id);
                if (user == null)
                    return new ApiResponse<UserDto>(false, "User not found");

                user.FirstName = command.FirstName;
                user.LastName = command.LastName;
                user.Gender = Enum.Parse<Models.GenderEnum>(command.Gender);
                user.Address = command.Address;
                user.EmailAddress = command.EmailAddress;
                user.PhoneNumber = command.PhoneNumber;
                user.DepartmentID = command.DepartmentID;
                user.UserRoleID = command.UserRoleID;

                await _dbContext.SaveChangesAsync();

                var updatedUser = await GetUserDtoById(user.Id);
                return new ApiResponse<UserDto>(true, "User updated successfully", updatedUser);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user");
                return new ApiResponse<UserDto>(false, "Error updating user");
            }
        }

        public async Task<ApiResponse<bool>> DeleteUserAsync(Guid id)
        {
            try
            {
                var user = await _dbContext.Users.FindAsync(id);
                if (user == null)
                    return new ApiResponse<bool>(false, "User not found");

                _dbContext.Users.Remove(user);
                await _dbContext.SaveChangesAsync();

                return new ApiResponse<bool>(true, "User deleted successfully", true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user");
                return new ApiResponse<bool>(false, "Error deleting user");
            }
        }

        public async Task<ApiResponse<UserAuthResponseDto>> AuthenticateAsync(UserLoginDto loginDto)
        {
            try
            {
                // Use a more targeted query that doesn't load related entities
                var user = await _dbContext.Users
                    .AsNoTracking()
                    .Where(u => u.EmailAddress == loginDto.EmailAddress)
                    .Select(u => new
                    {
                        u.Id,
                        u.UserId,
                        u.FirstName,
                        u.LastName,
                        u.EmailAddress,
                        u.Password,
                        RoleName = u.UserRole.UserRoleName
                    })
                    .FirstOrDefaultAsync();

                if (user == null)
                    return new ApiResponse<UserAuthResponseDto>(false, "User not found");

                // Add simple password verification
                if (user.Password != loginDto.Password) // TODO: Implement proper password hashing
                    return new ApiResponse<UserAuthResponseDto>(false, "Invalid password");

                var response = new UserAuthResponseDto(
                    user.Id,
                    user.UserId,
                    $"{user.FirstName} {user.LastName}",
                    user.EmailAddress,
                    user.RoleName,
                    "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c" // TODO: Implement proper JWT token generation
                );

                return new ApiResponse<UserAuthResponseDto>(true, "Authentication successful", response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during authentication");
                return new ApiResponse<UserAuthResponseDto>(false, "Error during authentication");
            }
        }

        public async Task<ApiResponse<PagedResponse<UserDto>>> GetUsersByDepartmentAsync(Guid departmentId, int page = 1, int pageSize = 10)
        {
            try
            {
                var query = _dbContext.Users
                    .Include(u => u.Department)
                    .Include(u => u.UserRole)
                    .Where(u => u.DepartmentID == departmentId)
                    .OrderBy(u => u.UserId)
                    .AsNoTracking();

                var totalItems = await query.CountAsync();
                var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

                var users = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(u => new UserDto(
                        u.Id,
                        u.UserId,
                        u.FirstName,
                        u.LastName,
                        u.Gender.ToString(),
                        u.Department!.DepartmentName,
                        u.UserRole!.UserRoleName,
                        u.Address,
                        u.EmailAddress,
                        u.PhoneNumber
                    ))
                    .ToListAsync();

                var pagedResponse = new PagedResponse<UserDto>(
                    users,
                    page,
                    pageSize,
                    totalItems,
                    totalPages
                );

                return new ApiResponse<PagedResponse<UserDto>>(true, "Users retrieved successfully", pagedResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving users by department");
                return new ApiResponse<PagedResponse<UserDto>>(false, "Error retrieving users by department");
            }
        }

        public async Task<ApiResponse<PagedResponse<UserDto>>> GetUsersByRoleAsync(Guid roleId, int page = 1, int pageSize = 10)
        {
            try
            {
                var query = _dbContext.Users
                    .Include(u => u.Department)
                    .Include(u => u.UserRole)
                    .Where(u => u.UserRoleID == roleId)
                    .OrderBy(u => u.UserId)
                    .AsNoTracking();

                var totalItems = await query.CountAsync();
                var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

                var users = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(u => new UserDto(
                        u.Id,
                        u.UserId,
                        u.FirstName,
                        u.LastName,
                        u.Gender.ToString(),
                        u.Department!.DepartmentName,
                        u.UserRole!.UserRoleName,
                        u.Address,
                        u.EmailAddress,
                        u.PhoneNumber
                    ))
                    .ToListAsync();

                var pagedResponse = new PagedResponse<UserDto>(
                    users,
                    page,
                    pageSize,
                    totalItems,
                    totalPages
                );

                return new ApiResponse<PagedResponse<UserDto>>(true, "Users retrieved successfully", pagedResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving users by role");
                return new ApiResponse<PagedResponse<UserDto>>(false, "Error retrieving users by role");
            }
        }

        public async Task<ApiResponse<UserDto>> UpdateUserByUserIdAsync(string userId, UpdateUserDto command)
        {
            try
            {
                var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.UserId == userId);
                if (user == null)
                    return new ApiResponse<UserDto>(false, "User not found");

                user.FirstName = command.FirstName;
                user.LastName = command.LastName;
                user.Gender = Enum.Parse<Models.GenderEnum>(command.Gender);
                user.Address = command.Address;
                user.EmailAddress = command.EmailAddress;
                user.PhoneNumber = command.PhoneNumber;
                user.DepartmentID = command.DepartmentID;
                user.UserRoleID = command.UserRoleID;

                await _dbContext.SaveChangesAsync();

                var updatedUser = await GetUserDtoById(user.Id);
                return new ApiResponse<UserDto>(true, "User updated successfully", updatedUser);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user");
                return new ApiResponse<UserDto>(false, "Error updating user");
            }
        }

        public async Task<ApiResponse<bool>> DeleteUserByUserIdAsync(string userId)
        {
            try
            {
                var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.UserId == userId);
                if (user == null)
                    return new ApiResponse<bool>(false, "User not found");

                _dbContext.Users.Remove(user);
                await _dbContext.SaveChangesAsync();

                return new ApiResponse<bool>(true, "User deleted successfully", true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user");
                return new ApiResponse<bool>(false, "Error deleting user");
            }
        }

        private async Task<UserDto?> GetUserDtoById(Guid id)
        {
            return await _dbContext.Users
                .Include(u => u.Department)
                .Include(u => u.UserRole)
                .Where(u => u.Id == id)
                .Select(u => new UserDto(
                    u.Id,
                    u.UserId,
                    u.FirstName,
                    u.LastName,
                    u.Gender.ToString(),
                    u.Department!.DepartmentName,
                    u.UserRole!.UserRoleName,
                    u.Address,
                    u.EmailAddress,
                    u.PhoneNumber
                ))
                .FirstOrDefaultAsync();
        }

        public async Task<ApiResponse<bool>> IsUserRegisteredByEmailAsync(string email)
        {
            try
            {
                _logger.LogInformation("Checking if user with email {Email} is registered", email);
                
                if (string.IsNullOrEmpty(email))
                {
                    return new ApiResponse<bool>(false, "Email is required", false);
                }
                
                var exists = await _dbContext.Users
                    .AsNoTracking()
                    .AnyAsync(u => u.EmailAddress == email);
                
                if (!exists)
                {
                    _logger.LogInformation("User with email {Email} not found", email);
                    return new ApiResponse<bool>(false, "User not found", false);
                }
                
                _logger.LogInformation("User with email {Email} found", email);
                return new ApiResponse<bool>(true, "User is registered", true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if user is registered by email {Email}", email);
                return new ApiResponse<bool>(false, "Error checking user registration", false);
            }
        }

        public async Task<ApiResponse<UserAuthResponseDto>> AuthenticateByEmailAsync(string email)
        {
            try
            {
                _logger.LogInformation("Authenticating user with email {Email}", email);
                
                if (string.IsNullOrEmpty(email))
                {
                    return new ApiResponse<UserAuthResponseDto>(false, "Email is required");
                }
                
                // Use a targeted query that doesn't load related entities
                var user = await _dbContext.Users
                    .AsNoTracking()
                    .Where(u => u.EmailAddress == email)
                    .Select(u => new
                    {
                        u.Id,
                        u.UserId,
                        u.FirstName,
                        u.LastName,
                        u.EmailAddress,
                        RoleName = u.UserRole.UserRoleName
                    })
                    .FirstOrDefaultAsync();

                if (user == null)
                {
                    _logger.LogWarning("User with email {Email} not found during Microsoft authentication", email);
                    return new ApiResponse<UserAuthResponseDto>(false, "User not found");
                }

                var response = new UserAuthResponseDto(
                    user.Id,
                    user.UserId,
                    $"{user.FirstName} {user.LastName}",
                    user.EmailAddress,
                    user.RoleName,
                    "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c" // TODO: Implement proper JWT token generation
                );

                _logger.LogInformation("Microsoft authentication successful for user {Email}", email);
                return new ApiResponse<UserAuthResponseDto>(true, "Authentication successful", response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Microsoft authentication for email {Email}", email);
                return new ApiResponse<UserAuthResponseDto>(false, "Error during authentication");
            }
        }
    }
}
