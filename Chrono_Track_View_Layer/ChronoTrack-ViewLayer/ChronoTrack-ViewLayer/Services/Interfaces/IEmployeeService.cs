using ChronoTrack_ViewLayer.Models;
using System;
using System.Threading.Tasks;

namespace ChronoTrack_ViewLayer.Services.Interfaces
{
    public interface IEmployeeService
    {
        Task<ServiceResponse<PagedResponse<EmployeeDto>>> GetAllEmployeesAsync(int page = 1, int pageSize = 10);
        Task<ServiceResponse<EmployeeDto>> GetEmployeeByIdAsync(string userId);
        Task<ServiceResponse<EmployeeDto>> CreateEmployeeAsync(CreateEmployeeDto command);
        Task<ServiceResponse<EmployeeDto>> UpdateEmployeeAsync(string userId, UpdateEmployeeDto command);
        Task<ServiceResponse<bool>> DeleteEmployeeAsync(string userId);
        Task<ServiceResponse<int>> GetEmployeeCountAsync();
        
        // Methods for Microsoft authentication
        Task<bool> IsUserRegisteredAsync(string email);
        Task<EmployeeDto> GetEmployeeByEmailAsync(string email);
    }
} 