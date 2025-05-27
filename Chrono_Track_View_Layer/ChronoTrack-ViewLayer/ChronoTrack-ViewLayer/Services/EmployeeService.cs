using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ChronoTrack_ViewLayer.Models;
using ChronoTrack_ViewLayer.Services.Interfaces;
using System.Collections.Generic;
using System.Linq;

namespace ChronoTrack_ViewLayer.Services
{
    public class EmployeeService : IEmployeeService
    {
        // Use the HttpClientFactory instead of managing our own HttpClient
        private readonly HttpClient _httpClient;

        public EmployeeService()
        {
            // Get the shared HttpClient instance from the factory
            _httpClient = HttpClientFactory.Client;
        }

        public async Task<ServiceResponse<EmployeeDto>> CreateEmployeeAsync(CreateEmployeeDto employee)
        {
            try
            {
                // Log the request data
                Console.WriteLine($"Attempting to create employee with: UserId={employee.UserId}, Name={employee.FirstName} {employee.LastName}");
                
                // Determine proper gender enum value
                string genderEnumValue;
                switch (employee.Gender.ToLower())
                {
                    case "male":
                    case "0":
                        genderEnumValue = "Male";
                        break;
                    case "female":
                    case "1":
                        genderEnumValue = "Female";
                        break;
                    case "other":
                    case "2":
                        genderEnumValue = "Other";
                        break;
                    default:
                        genderEnumValue = "Male"; // Default
                        break;
                }
                
                Console.WriteLine($"Gender mapped to: {genderEnumValue}");
                
                // Log department and role IDs from the employee object
                Console.WriteLine($"Department ID: {employee.DepartmentID}");
                Console.WriteLine($"Role ID: {employee.UserRoleID}");
                
                // Create a dictionary with the exact properties needed
                var employeeDict = new Dictionary<string, object>
                {
                    { "userId", employee.UserId },
                    { "firstName", employee.FirstName },
                    { "lastName", employee.LastName },
                    { "gender", genderEnumValue }, // Use the enum name
                    { "address", employee.Address },
                    { "emailAddress", employee.EmailAddress },
                    { "phoneNumber", employee.PhoneNumber },
                    { "password", employee.Password },
                    { "departmentID", employee.DepartmentID },
                    { "userRoleID", employee.UserRoleID },
                    { "userTypeID", employee.UserTypeID }
                };
                
                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true
                };
                
                var json = JsonSerializer.Serialize(employeeDict, options);
                Console.WriteLine($"Sending JSON: {json}");
                
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                Console.WriteLine("Sending API request to: " + _httpClient.BaseAddress + "api/employees");
                
                // Use the retry logic for the HTTP request
                var response = await HttpClientFactory.ExecuteWithRetry(() => 
                    _httpClient.PostAsync("/api/employees", content));
                
                Console.WriteLine($"API Response Status: {response.StatusCode}");
                
                if (response.IsSuccessStatusCode)
                {
                    var resultString = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"API Success Response: {resultString}");
                    
                    try
                    {
                        // Use JsonSerializer instead of ReadFromJsonAsync
                        var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                        var apiResponse = JsonSerializer.Deserialize<ApiResponse>(resultString, jsonOptions);
                        
                        if (apiResponse != null && apiResponse.Success && apiResponse.Data != null)
                        {
                            var resultJson = apiResponse.Data.ToString();
                            var employeeDto = JsonSerializer.Deserialize<EmployeeDto>(resultJson, jsonOptions);
                                
                            return new ServiceResponse<EmployeeDto>
                            {
                                Success = true,
                                Data = employeeDto
                            };
                        }
                        
                        return new ServiceResponse<EmployeeDto>
                        {
                            Success = true,
                            Message = "Employee created, but response could not be parsed."
                        };
                    }
                    catch (JsonException ex)
                    {
                        Console.WriteLine($"JSON deserialization failed: {ex.Message}");
                        return new ServiceResponse<EmployeeDto>
                        {
                            Success = true, // The request was successful
                            Message = "Employee created, but response could not be parsed."
                        };
                    }
                }
                else
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"API Error Response: {errorContent}");
                    
                    // Try to parse error response
                    try 
                    {
                        var errorResponse = JsonSerializer.Deserialize<ApiResponse<object>>(errorContent, options);
                        if (errorResponse != null && !string.IsNullOrEmpty(errorResponse.Message))
                        {
                            return new ServiceResponse<EmployeeDto>
                            {
                                Success = false,
                                Message = errorResponse.Message
                            };
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Could not parse error response: {ex.Message}");
                    }
                    
                    return new ServiceResponse<EmployeeDto>
                    {
                        Success = false,
                        Message = $"Failed to create employee. Status code: {response.StatusCode}. Details: {errorContent}"
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in CreateEmployeeAsync: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                
                return new ServiceResponse<EmployeeDto>
                {
                    Success = false,
                    Message = $"Error creating employee: {ex.Message}"
                };
            }
        }

        // Method to handle employee by ID
        public async Task<ServiceResponse<EmployeeDto>> GetEmployeeByIdAsync(string userId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/api/employees/{userId}");
                
                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var result = JsonSerializer.Deserialize<ApiResponse<EmployeeDto>>(jsonString, options);
                    
                    if (result != null && result.Data != null)
                    {
                        return new ServiceResponse<EmployeeDto>
                        {
                            Success = true,
                            Data = result.Data
                        };
                    }
                    
                    return new ServiceResponse<EmployeeDto>
                    {
                        Success = false,
                        Message = "Failed to parse employee data"
                    };
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                return new ServiceResponse<EmployeeDto>
                {
                    Success = false,
                    Message = $"Failed to retrieve employee. Status code: {response.StatusCode}, Details: {errorContent}"
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Get employee failed: {ex.Message}");
                return new ServiceResponse<EmployeeDto>
                {
                    Success = false,
                    Message = $"Error retrieving employee: {ex.Message}"
                };
            }
        }

        // Update using string userId
        public async Task<ServiceResponse<EmployeeDto>> UpdateEmployeeAsync(string userId, UpdateEmployeeDto employee)
        {
            try
            {
                Console.WriteLine($"Updating employee with userId: {userId}");
                
                // Manually serialize to control the JSON output
                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true
                };
                
                // Create a dictionary with the exact properties needed for update
                var employeeDict = new Dictionary<string, object>
                {
                    { "userId", employee.UserId },
                    { "firstName", employee.FirstName },
                    { "lastName", employee.LastName },
                    { "gender", employee.Gender },
                    { "address", employee.Address },
                    { "emailAddress", employee.EmailAddress },
                    { "phoneNumber", employee.PhoneNumber },
                    { "departmentID", employee.DepartmentID },
                    { "userRoleID", employee.UserRoleID }
                };
                
                var json = JsonSerializer.Serialize(employeeDict, options);
                Console.WriteLine($"Sending JSON: {json}");
                
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                // Use userId directly in the URL
                var response = await _httpClient.PutAsync($"/api/employees/{userId}", content);
                
                Console.WriteLine($"Update API Response Status: {response.StatusCode}");
                
                if (response.IsSuccessStatusCode)
                {
                    var resultString = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"API Success Response: {resultString}");
                    
                    try
                    {
                        // Use JsonSerializer instead of ReadFromJsonAsync
                        var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                        var apiResponse = JsonSerializer.Deserialize<ApiResponse>(resultString, jsonOptions);
                        
                        if (apiResponse != null && apiResponse.Success && apiResponse.Data != null)
                        {
                            var resultJson = apiResponse.Data.ToString();
                            var employeeDto = JsonSerializer.Deserialize<EmployeeDto>(resultJson, jsonOptions);
                                
                            return new ServiceResponse<EmployeeDto>
                            {
                                Success = true,
                                Data = employeeDto
                            };
                        }
                        
                        return new ServiceResponse<EmployeeDto>
                        {
                            Success = true,
                            Message = "Employee updated, but response could not be parsed."
                        };
                    }
                    catch (JsonException ex)
                    {
                        Console.WriteLine($"JSON deserialization failed: {ex.Message}");
                        return new ServiceResponse<EmployeeDto>
                        {
                            Success = true, // The request was successful
                            Message = "Employee updated, but response could not be parsed."
                        };
                    }
                }
                else
                {
                    // Try to get detailed error message from response
                    string errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Server error response: {errorContent}");
                    
                    return new ServiceResponse<EmployeeDto>
                    {
                        Success = false,
                        Message = $"Failed to update employee. Status code: {response.StatusCode}. Details: {errorContent}"
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Update employee failed: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return new ServiceResponse<EmployeeDto>
                {
                    Success = false,
                    Message = $"Error updating employee: {ex.Message}"
                };
            }
        }

        public async Task<ServiceResponse<PagedResponse<EmployeeDto>>> GetAllEmployeesAsync(int page = 1, int pageSize = 10)
        {
            try
            {
                Console.WriteLine($"Calling API to get employees for page {page}, pageSize {pageSize}");
                var response = await _httpClient.GetAsync($"/api/employees?page={page}&pageSize={pageSize}");
                
                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"API Response: {jsonString}");
                    
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    
                    var apiResponse = JsonSerializer.Deserialize<ApiResponse>(jsonString, options);
                    
                    if (apiResponse != null && apiResponse.Success && apiResponse.Data != null)
                    {
                        var dataJson = apiResponse.Data.ToString();
                        var pagedResult = JsonSerializer.Deserialize<PagedResult<EmployeeDto>>(dataJson, options);
                        
                        if (pagedResult != null)
                        {
                            // Convert PagedResult to PagedResponse
                            var pagedResponse = new PagedResponse<EmployeeDto>
                            {
                                Items = pagedResult.Items ?? new List<EmployeeDto>(),
                                CurrentPage = pagedResult.Page,
                                TotalPages = pagedResult.TotalPages,
                                PageSize = pagedResult.PageSize,
                                TotalItems = pagedResult.TotalItems
                            };
                            
                            Console.WriteLine($"Retrieved {pagedResponse.Items.Count} employees");
                            Console.WriteLine($"Total Items: {pagedResponse.TotalItems}");
                            Console.WriteLine($"Current Page: {pagedResponse.CurrentPage}");
                            Console.WriteLine($"Total Pages: {pagedResponse.TotalPages}");
                            
                            return new ServiceResponse<PagedResponse<EmployeeDto>>
                            {
                                Success = true,
                                Data = pagedResponse
                            };
                        }
                    }
                    
                    return new ServiceResponse<PagedResponse<EmployeeDto>>
                    {
                        Success = false,
                        Message = "Failed to parse API response"
                    };
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"API Error Response: {errorContent}");
                return new ServiceResponse<PagedResponse<EmployeeDto>>
                {
                    Success = false,
                    Message = $"Failed to retrieve employees. Status code: {response.StatusCode}, Details: {errorContent}"
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Get employees failed: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return new ServiceResponse<PagedResponse<EmployeeDto>>
                {
                    Success = false,
                    Message = $"Error retrieving employees: {ex.Message}"
                };
            }
        }

        // Implement the DeleteEmployeeAsync method using userId
        public async Task<ServiceResponse<bool>> DeleteEmployeeAsync(string userId)
        {
            try
            {
                Console.WriteLine("===================================================");
                Console.WriteLine("DELETING EMPLOYEE USING USERID");
                Console.WriteLine($"Deleting employee with UserID: {userId}");
                Console.WriteLine("===================================================");
                
                // Construct the exact URL for the API call with userId
                var requestUrl = $"/api/employees/{userId}";
                Console.WriteLine($"Making DELETE request to: {_httpClient.BaseAddress}{requestUrl}");
                
                // Use the retry logic for the HTTP request
                var response = await HttpClientFactory.ExecuteWithRetry(() => 
                    _httpClient.DeleteAsync(requestUrl));
                
                Console.WriteLine($"Delete operation response status code: {response.StatusCode}");
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Successful deletion response: {responseContent}");
                    
                    return new ServiceResponse<bool>
                    {
                        Success = true,
                        Data = true,
                        Message = "Employee deleted successfully"
                    };
                }
                else
                {
                    // Try to get detailed error message from response
                    string errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Server error response: {errorContent}");
                    
                    return new ServiceResponse<bool>
                    {
                        Success = false,
                        Data = false,
                        Message = $"Failed to delete employee. Status code: {response.StatusCode}. Details: {errorContent}"
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in DeleteEmployeeByUserIdAsync: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                
                return new ServiceResponse<bool>
                {
                    Success = false,
                    Data = false,
                    Message = $"Error deleting employee: {ex.Message}"
                };
            }
        }

        // New method to get employee count from the API
        public async Task<ServiceResponse<int>> GetEmployeeCountAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/api/employees/count");
                
                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var result = JsonSerializer.Deserialize<ApiResponse<int>>(jsonString, options);
                    
                    if (result != null)
                    {
                        return new ServiceResponse<int>
                        {
                            Success = true,
                            Data = result.Data
                        };
                    }
                    
                    return new ServiceResponse<int>
                    {
                        Success = false,
                        Message = "Could not parse employee count from response"
                    };
                }
                
                string errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"API Error Response for GetEmployeeCountAsync: {errorContent}");
                
                return new ServiceResponse<int>
                {
                    Success = false,
                    Message = $"Failed to get employee count. Status code: {response.StatusCode}"
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in GetEmployeeCountAsync: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                
                return new ServiceResponse<int>
                {
                    Success = false,
                    Message = $"Error getting employee count: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Check if a user with the given email is registered in the system
        /// </summary>
        public async Task<bool> IsUserRegisteredAsync(string email)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email))
                    return false;
                
                Console.WriteLine($"Checking if email is registered: {email}");
                
                // Call the API to check if email exists
                var response = await _httpClient.GetAsync($"/api/employees/email/exists/{Uri.EscapeDataString(email)}");
                
                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var result = JsonSerializer.Deserialize<ApiResponse<bool>>(jsonString, options);
                    
                    if (result != null && result.Success)
                    {
                        Console.WriteLine($"Email check result: {result.Data}");
                        return result.Data;
                    }
                    
                    Console.WriteLine("Email check API call succeeded but returned unsuccessful result");
                    return false;
                }
                
                // If API endpoint doesn't exist, fall back to getting all employees and checking
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    Console.WriteLine("Email exists endpoint not found, falling back to getAllEmployees");
                    var allEmployeesResponse = await GetAllEmployeesAsync(1, 1000); // Get all employees
                    
                    if (allEmployeesResponse.Success && allEmployeesResponse.Data?.Items != null)
                    {
                        var isRegistered = allEmployeesResponse.Data.Items.Any(e => 
                            e.EmailAddress != null && 
                            e.EmailAddress.Equals(email, StringComparison.OrdinalIgnoreCase));
                        
                        Console.WriteLine($"Email exists check from all employees: {isRegistered}");
                        return isRegistered;
                    }
                }
                
                string errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"API Error Response for IsUserRegisteredAsync: {errorContent}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in IsUserRegisteredAsync: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return false;
            }
        }
        
        /// <summary>
        /// Get employee details by email address
        /// </summary>
        public async Task<EmployeeDto> GetEmployeeByEmailAsync(string email)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email))
                    return null;
                
                Console.WriteLine($"Getting employee by email: {email}");
                
                // Call the API to get employee by email
                var response = await _httpClient.GetAsync($"/api/employees/email/{Uri.EscapeDataString(email)}");
                
                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var result = JsonSerializer.Deserialize<ApiResponse<EmployeeDto>>(jsonString, options);
                    
                    if (result != null && result.Success && result.Data != null)
                    {
                        Console.WriteLine($"Retrieved employee: {result.Data.EmployeeName}");
                        return result.Data;
                    }
                    
                    Console.WriteLine("GetEmployeeByEmail API call succeeded but returned null or unsuccessful result");
                    return null;
                }
                
                // If API endpoint doesn't exist, fall back to getting all employees and finding by email
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    Console.WriteLine("GetEmployeeByEmail endpoint not found, falling back to getAllEmployees");
                    var allEmployeesResponse = await GetAllEmployeesAsync(1, 1000); // Get all employees
                    
                    if (allEmployeesResponse.Success && allEmployeesResponse.Data?.Items != null)
                    {
                        var employee = allEmployeesResponse.Data.Items.FirstOrDefault(e => 
                            e.EmailAddress != null && 
                            e.EmailAddress.Equals(email, StringComparison.OrdinalIgnoreCase));
                        
                        if (employee != null)
                        {
                            Console.WriteLine($"Found employee by email from all employees: {employee.EmployeeName}");
                            return employee;
                        }
                    }
                }
                
                string errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"API Error Response for GetEmployeeByEmailAsync: {errorContent}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in GetEmployeeByEmailAsync: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return null;
            }
        }

        // Additional methods that are not part of the interface...
    }
} 