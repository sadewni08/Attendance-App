using ChronoTrack.DTOs;
using ChronoTrack.DTOs.Common;
using ChronoTrack.Persistence;
using ChronoTrack.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ChronoTrack.Services
{
    public class DepartmentService : IDepartmentService
    {
        private readonly ChronoTrackDbContext _dbContext;
        private readonly ILogger<DepartmentService> _logger;

        public DepartmentService(ChronoTrackDbContext dbContext, ILogger<DepartmentService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<ApiResponse<DepartmentDto>> CreateDepartmentAsync(CreateDepartmentDto command)
        {
            try
            {
                var department = new Models.Department
                {
                    DepartmentName = command.DepartmentName,
                    Description = command.Description
                };

                await _dbContext.Departments.AddAsync(department);
                await _dbContext.SaveChangesAsync();

                var createdDepartment = await GetDepartmentDtoById(department.Id);
                return new ApiResponse<DepartmentDto>(true, "Department created successfully", createdDepartment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating department");
                return new ApiResponse<DepartmentDto>(false, "Error creating department");
            }
        }

        public async Task<ApiResponse<DepartmentDto>> GetDepartmentByIdAsync(Guid id)
        {
            try
            {
                var department = await GetDepartmentDtoById(id);
                if (department == null)
                    return new ApiResponse<DepartmentDto>(false, "Department not found");

                return new ApiResponse<DepartmentDto>(true, "Department retrieved successfully", department);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving department");
                return new ApiResponse<DepartmentDto>(false, "Error retrieving department");
            }
        }

        public async Task<ApiResponse<PagedResponse<DepartmentDto>>> GetAllDepartmentsAsync(int page = 1, int pageSize = 10)
        {
            try
            {
                var query = _dbContext.Departments
                    .OrderBy(d => d.DepartmentName)
                    .AsNoTracking();
                var totalItems = await query.CountAsync();
                var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

                var departments = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(d => new DepartmentDto(
                        d.Id,
                        d.DepartmentName,
                        d.Description,
                        d.Users.Count
                    ))
                    .ToListAsync();

                var pagedResponse = new PagedResponse<DepartmentDto>(
                    departments,
                    page,
                    pageSize,
                    totalItems,
                    totalPages
                );

                return new ApiResponse<PagedResponse<DepartmentDto>>(true, "Departments retrieved successfully", pagedResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving departments");
                return new ApiResponse<PagedResponse<DepartmentDto>>(false, "Error retrieving departments");
            }
        }

        public async Task<ApiResponse<DepartmentDto>> UpdateDepartmentAsync(Guid id, UpdateDepartmentDto command)
        {
            try
            {
                var department = await _dbContext.Departments.FindAsync(id);
                if (department == null)
                    return new ApiResponse<DepartmentDto>(false, "Department not found");

                department.DepartmentName = command.DepartmentName;
                department.Description = command.Description;

                await _dbContext.SaveChangesAsync();

                var updatedDepartment = await GetDepartmentDtoById(department.Id);
                return new ApiResponse<DepartmentDto>(true, "Department updated successfully", updatedDepartment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating department");
                return new ApiResponse<DepartmentDto>(false, "Error updating department");
            }
        }

        public async Task<ApiResponse<bool>> DeleteDepartmentAsync(Guid id)
        {
            try
            {
                var department = await _dbContext.Departments.FindAsync(id);
                if (department == null)
                    return new ApiResponse<bool>(false, "Department not found");

                _dbContext.Departments.Remove(department);
                await _dbContext.SaveChangesAsync();

                return new ApiResponse<bool>(true, "Department deleted successfully", true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting department");
                return new ApiResponse<bool>(false, "Error deleting department");
            }
        }

        public async Task<ApiResponse<List<DepartmentDto>>> GetActiveDepartmentsAsync()
        {
            try
            {
                var departments = await _dbContext.Departments
                    .AsNoTracking()
                    .Select(d => new DepartmentDto(
                        d.Id,
                        d.DepartmentName,
                        d.Description,
                        d.Users.Count
                    ))
                    .ToListAsync();

                return new ApiResponse<List<DepartmentDto>>(true, "Active departments retrieved successfully", departments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving active departments");
                return new ApiResponse<List<DepartmentDto>>(false, "Error retrieving active departments");
            }
        }

        public async Task<ApiResponse<DepartmentSummaryDto>> GetDepartmentSummaryAsync(Guid id, DateTime? fromDate = null, DateTime? toDate = null)
        {
            try
            {
                var department = await _dbContext.Departments
                    .Include(d => d.Users)
                    .ThenInclude(u => u.Attendances.Where(a => 
                        (!fromDate.HasValue || a.AttendanceDate >= fromDate.Value.Date) &&
                        (!toDate.HasValue || a.AttendanceDate <= toDate.Value.Date)))
                    .FirstOrDefaultAsync(d => d.Id == id);

                if (department == null)
                    return new ApiResponse<DepartmentSummaryDto>(false, "Department not found");

                var employeeCount = department.Users.Count;
                var attendances = department.Users.SelectMany(u => u.Attendances).ToList();

                var averageAttendance = employeeCount > 0
                    ? (double)attendances.Count / employeeCount
                    : 0;

                var averageWorkingHours = attendances.Any()
                    ? attendances.Average(a => (a.CheckOutTime - a.CheckInTime).TotalHours)
                    : 0;

                var summary = new DepartmentSummaryDto(
                    department.Id,
                    department.DepartmentName,
                    employeeCount,
                    Math.Round(averageAttendance, 2),
                    Math.Round(averageWorkingHours, 2)
                );

                return new ApiResponse<DepartmentSummaryDto>(true, "Department summary retrieved successfully", summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving department summary");
                return new ApiResponse<DepartmentSummaryDto>(false, "Error retrieving department summary");
            }
        }

        public async Task<ApiResponse<List<DepartmentSummaryDto>>> GetAllDepartmentsSummaryAsync(DateTime? fromDate = null, DateTime? toDate = null)
        {
            try
            {
                var departments = await _dbContext.Departments
                    .Include(d => d.Users)
                    .ThenInclude(u => u.Attendances.Where(a => 
                        (!fromDate.HasValue || a.AttendanceDate >= fromDate.Value.Date) &&
                        (!toDate.HasValue || a.AttendanceDate <= toDate.Value.Date)))
                    .ToListAsync();

                var summaries = departments.Select(department =>
                {
                    var employeeCount = department.Users.Count;
                    var attendances = department.Users.SelectMany(u => u.Attendances).ToList();

                    var averageAttendance = employeeCount > 0
                        ? (double)attendances.Count / employeeCount
                        : 0;

                    var averageWorkingHours = attendances.Any()
                        ? attendances.Average(a => (a.CheckOutTime - a.CheckInTime).TotalHours)
                        : 0;

                    return new DepartmentSummaryDto(
                        department.Id,
                        department.DepartmentName,
                        employeeCount,
                        Math.Round(averageAttendance, 2),
                        Math.Round(averageWorkingHours, 2)
                    );
                }).ToList();

                return new ApiResponse<List<DepartmentSummaryDto>>(true, "Department summaries retrieved successfully", summaries);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving department summaries");
                return new ApiResponse<List<DepartmentSummaryDto>>(false, "Error retrieving department summaries");
            }
        }

        private async Task<DepartmentDto?> GetDepartmentDtoById(Guid id)
        {
            return await _dbContext.Departments
                .Where(d => d.Id == id)
                .Select(d => new DepartmentDto(
                    d.Id,
                    d.DepartmentName,
                    d.Description,
                    d.Users.Count
                ))
                .FirstOrDefaultAsync();
        }
    }
} 