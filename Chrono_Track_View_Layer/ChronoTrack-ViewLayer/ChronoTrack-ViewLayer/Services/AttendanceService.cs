using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using ChronoTrack_ViewLayer.Models;
using System.Text.Json;
using System.Text;
using System.Collections.Generic;
using ChronoTrack_ViewLayer.Services.Interfaces;
using System.Linq;

namespace ChronoTrack_ViewLayer.Services
{
    public class AttendanceService : IAttendanceService
    {
        private HttpClient _httpClient;
        private readonly string _baseUrl = "https://localhost:7222/api/attendance";
        private readonly JsonSerializerOptions _jsonOptions;

        public AttendanceService()
        {
            try
            {
                _httpClient = HttpClientFactory.Client;

                // Configure JSON serialization options
                _jsonOptions = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true,
                    PropertyNameCaseInsensitive = true
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing AttendanceService: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        // Helper method to ensure the HttpClient is set up correctly
        private async Task SetupHttpClientAsync()
        {
            // Ensure we have a valid HttpClient
            if (_httpClient == null)
            {
                _httpClient = HttpClientFactory.Client;
            }

            // Any other setup logic can go here
            await Task.CompletedTask; // This ensures the method is truly async
        }

        /// <summary>
        /// Gets the attendance history for a specific user
        /// </summary>
        public async Task<ServiceResponse<PagedResponse<AttendanceDto>>> GetUserAttendanceHistoryAsync(
            string userId,
            DateTime? startDate = null,
            DateTime? endDate = null,
            int page = 1,
            int pageSize = 10)
        {
            if (string.IsNullOrEmpty(userId))
            {
                Console.WriteLine("GetUserAttendanceHistoryAsync error: User ID is empty");
                return new ServiceResponse<PagedResponse<AttendanceDto>>
                {
                    Success = false,
                    Message = "User ID cannot be empty"
                };
            }

            HttpResponseMessage response = null;

            try
            {
                var apiUrl = $"{_baseUrl}/user/{userId}";

                // Add query parameters for date filtering and pagination
                var queryParams = new List<string>();

                if (startDate.HasValue)
                {
                    queryParams.Add($"startDate={startDate.Value:yyyy-MM-dd}");
                }

                if (endDate.HasValue)
                {
                    queryParams.Add($"endDate={endDate.Value:yyyy-MM-dd}");
                }

                queryParams.Add($"page={page}");
                queryParams.Add($"pageSize={pageSize}");

                if (queryParams.Any())
                {
                    apiUrl += $"?{string.Join("&", queryParams)}";
                }

                Console.WriteLine($"Calling API: GET {apiUrl}");

                response = await _httpClient.GetAsync(apiUrl);
                Console.WriteLine($"Get attendance history API call response: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Raw response: {responseContent}");

                    try
                    {
                        var apiResponse = JsonSerializer.Deserialize<ApiResponse>(responseContent, _jsonOptions);

                        if (apiResponse != null)
                        {
                            Console.WriteLine($"Deserialized API response: Success={apiResponse.Success}, Message={apiResponse.Message ?? "null"}");

                            if (apiResponse.Success && apiResponse.Data != null)
                            {
                                try
                                {
                                    var resultJson = apiResponse.Data.ToString();
                                    Console.WriteLine($"PagedResponse<AttendanceDto> data JSON: {resultJson}");

                                    var result = JsonSerializer.Deserialize<PagedResponse<AttendanceDto>>(resultJson, _jsonOptions);
                                    if (result != null)
                                    {
                                        return new ServiceResponse<PagedResponse<AttendanceDto>>
                                        {
                                            Success = true,
                                            Data = result
                                        };
                                    }
                                }
                                catch (JsonException je)
                                {
                                    Console.WriteLine($"JSON parsing error: {je.Message}");
                                    return new ServiceResponse<PagedResponse<AttendanceDto>>
                                    {
                                        Success = false,
                                        Message = $"Data parsing error: {je.Message}"
                                    };
                                }
                            }

                            return new ServiceResponse<PagedResponse<AttendanceDto>>
                            {
                                Success = apiResponse.Success,
                                Message = apiResponse.Message ?? "API call succeeded but returned no data"
                            };
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error parsing API response: {ex.Message}");
                        return new ServiceResponse<PagedResponse<AttendanceDto>>
                        {
                            Success = false,
                            Message = $"Error parsing API response: {ex.Message}"
                        };
                    }
                }

                var errorMessage = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Get attendance history API call failed: {response.StatusCode}, Error: {errorMessage}");
                return new ServiceResponse<PagedResponse<AttendanceDto>>
                {
                    Success = false,
                    Message = $"Failed to get attendance history: {errorMessage}"
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during get attendance history: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return new ServiceResponse<PagedResponse<AttendanceDto>>
                {
                    Success = false,
                    Message = $"Error during get attendance history: {ex.Message}"
                };
            }
            finally
            {
                response?.Dispose();
            }
        }

        /// <summary>
        /// Checks in a user for the current day
        /// </summary>
        public async Task<ServiceResponse<AttendanceDto>> CheckInAsync(string userId, CreateAttendanceDto command)
        {
            if (string.IsNullOrEmpty(userId))
            {
                Console.WriteLine("CheckInAsync error: User ID is empty");
                return new ServiceResponse<AttendanceDto>
                {
                    Success = false,
                    Message = "User ID cannot be empty"
                };
            }

            HttpResponseMessage response = null;

            try
            {
                await SetupHttpClientAsync();

                string apiUrl = $"{_baseUrl}/{userId}/checkin";

                var json = JsonSerializer.Serialize(command, _jsonOptions);
                using var content = new StringContent(json, Encoding.UTF8, "application/json");

                Console.WriteLine($"Calling API: POST {apiUrl}");
                Console.WriteLine($"Request body: {json}");

                response = await _httpClient.PostAsync(apiUrl, content);

                Console.WriteLine($"Check-in API call response: {response.StatusCode}");

                var responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Check-in API raw response: {responseContent}");

                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        var apiResponse = JsonSerializer.Deserialize<ApiResponse>(responseContent, _jsonOptions);

                        if (apiResponse != null)
                        {
                            Console.WriteLine($"Deserialized API response: Success={apiResponse.Success}, Message={apiResponse.Message ?? "null"}");

                            if (apiResponse.Success && apiResponse.Data != null)
                            {
                                try
                                {
                                    var resultJson = apiResponse.Data.ToString();
                                    Console.WriteLine($"AttendanceDto data JSON: {resultJson}");

                                    var result = JsonSerializer.Deserialize<AttendanceDto>(resultJson, _jsonOptions);
                                    if (result != null)
                                    {
                                        return new ServiceResponse<AttendanceDto>
                                        {
                                            Success = true,
                                            Data = result
                                        };
                                    }
                                }
                                catch (JsonException je)
                                {
                                    Console.WriteLine($"JSON parsing error: {je.Message}");
                                    return new ServiceResponse<AttendanceDto>
                                    {
                                        Success = false,
                                        Message = $"Data parsing error: {je.Message}"
                                    };
                                }
                            }

                            return new ServiceResponse<AttendanceDto>
                            {
                                Success = apiResponse.Success,
                                Message = apiResponse.Message ?? "Check-in call succeeded but returned no data"
                            };
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error parsing API response: {ex.Message}");
                        return new ServiceResponse<AttendanceDto>
                        {
                            Success = false,
                            Message = $"Error parsing API response: {ex.Message}"
                        };
                    }
                }

                // Handle error responses from the API
                string errorMessage = $"Failed to check in: {response.StatusCode}";
                try
                {
                    var errorResponse = JsonSerializer.Deserialize<ApiResponse>(responseContent, _jsonOptions);
                    if (errorResponse != null && !string.IsNullOrEmpty(errorResponse.Message))
                    {
                        errorMessage = errorResponse.Message;
                        Console.WriteLine($"API error: {errorMessage}");
                    }
                }
                catch (Exception parseEx)
                {
                    Console.WriteLine($"Error parsing error response: {parseEx.Message}");
                }

                return new ServiceResponse<AttendanceDto>
                {
                    Success = false,
                    Message = errorMessage
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during check-in: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return new ServiceResponse<AttendanceDto>
                {
                    Success = false,
                    Message = $"Error during check-in: {ex.Message}"
                };
            }
            finally
            {
                response?.Dispose();
            }
        }

        /// <summary>
        /// Checks out a user for a specific attendance record
        /// </summary>
        public async Task<ServiceResponse<AttendanceDto>> CheckOutAsync(string userId, string attendanceId, UpdateAttendanceDto command)
        {
            if (string.IsNullOrEmpty(userId))
            {
                Console.WriteLine("CheckOutAsync error: User ID is empty");
                return new ServiceResponse<AttendanceDto>
                {
                    Success = false,
                    Message = "User ID cannot be empty"
                };
            }

            if (string.IsNullOrEmpty(attendanceId))
            {
                Console.WriteLine("CheckOutAsync error: Attendance ID is empty");
                return new ServiceResponse<AttendanceDto>
                {
                    Success = false,
                    Message = "Attendance ID cannot be empty"
                };
            }

            // Ensure the date is set in the command and it's in UTC
            command.AttendanceDate = DateTime.SpecifyKind(command.AttendanceDate.Date, DateTimeKind.Utc);

            HttpResponseMessage response = null;

            try
            {
                var json = JsonSerializer.Serialize(command, _jsonOptions);
                using var content = new StringContent(json, Encoding.UTF8, "application/json");

                var apiUrl = $"{_baseUrl}/{userId}/checkout/{attendanceId}";
                Console.WriteLine($"Calling API: PUT {apiUrl}");
                Console.WriteLine($"Request body: {json}");

                response = await _httpClient.PutAsync(apiUrl, content);

                Console.WriteLine($"Check-out API call response: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Raw response: {responseContent}");

                    try
                    {
                        var apiResponse = JsonSerializer.Deserialize<ApiResponse>(responseContent, _jsonOptions);

                        if (apiResponse != null && apiResponse.Success && apiResponse.Data != null)
                        {
                            try
                            {
                                var resultJson = apiResponse.Data.ToString();
                                Console.WriteLine($"AttendanceDto data JSON: {resultJson}");

                                var result = JsonSerializer.Deserialize<AttendanceDto>(resultJson, _jsonOptions);
                                if (result != null)
                                {
                                    return new ServiceResponse<AttendanceDto>
                                    {
                                        Success = true,
                                        Data = result
                                    };
                                }
                            }
                            catch (JsonException je)
                            {
                                Console.WriteLine($"JSON parsing error: {je.Message}");
                                return new ServiceResponse<AttendanceDto>
                                {
                                    Success = false,
                                    Message = $"Data parsing error: {je.Message}"
                                };
                            }
                        }

                        return new ServiceResponse<AttendanceDto>
                        {
                            Success = apiResponse?.Success ?? false,
                            Message = apiResponse?.Message ?? "Check-out call succeeded but returned no data"
                        };
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error parsing API response: {ex.Message}");
                        return new ServiceResponse<AttendanceDto>
                        {
                            Success = false,
                            Message = $"Error parsing API response: {ex.Message}"
                        };
                    }
                }

                var errorMessage = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Check-out API call failed: {response.StatusCode}, Error: {errorMessage}");
                return new ServiceResponse<AttendanceDto>
                {
                    Success = false,
                    Message = $"Failed to check out: {errorMessage}"
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during check-out: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return new ServiceResponse<AttendanceDto>
                {
                    Success = false,
                    Message = $"Error during check-out: {ex.Message}"
                };
            }
            finally
            {
                response?.Dispose();
            }
        }

        /// <summary>
        /// Auto check-out users who haven't checked out by midnight
        /// </summary>
        public async Task<ServiceResponse<bool>> AutoCheckoutUsersAsync()
        {
            try
            {
                // Get all users who are still checked in for yesterday
                var historyResponse = await GetAllActiveAttendancesAsync();

                if (!historyResponse.Success || historyResponse.Data == null)
                {
                    return new ServiceResponse<bool>
                    {
                        Success = false,
                        Message = historyResponse.Message ?? "Failed to get active attendances"
                    };
                }

                bool allSuccess = true;
                string errorMessages = string.Empty;

                // Process each active attendance from yesterday
                foreach (var attendance in historyResponse.Data)
                {
                    try
                    {
                        // Auto check-out with 9 hours after check-in time
                        var autoCheckOutTime = attendance.CheckInTime.Add(TimeSpan.FromHours(9));

                        // Create checkout data with date and time
                        var checkOutData = new UpdateAttendanceDto(attendance.AttendanceDate, autoCheckOutTime);
                        var json = JsonSerializer.Serialize(checkOutData, _jsonOptions);

                        using (var content = new StringContent(json, Encoding.UTF8, "application/json"))
                        {
                            using (var response = await _httpClient.PutAsync($"{_baseUrl}/{attendance.UserId}/checkout", content))
                            {
                                if (!response.IsSuccessStatusCode)
                                {
                                    allSuccess = false;
                                    var error = await response.Content.ReadAsStringAsync();
                                    errorMessages += $"Failed to auto check-out user {attendance.UserId}: {error}. ";
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        allSuccess = false;
                        errorMessages += $"Error processing user {attendance.UserId}: {ex.Message}. ";
                        Console.WriteLine($"Error auto-checking out user {attendance.UserId}: {ex.Message}");
                    }
                }

                return new ServiceResponse<bool>
                {
                    Success = allSuccess,
                    Message = allSuccess ? "Auto check-out successful" : $"Some auto check-outs failed: {errorMessages}",
                    Data = allSuccess
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during auto check-out: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return new ServiceResponse<bool>
                {
                    Success = false,
                    Message = $"Error during auto check-out: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Gets all active attendances (users who haven't checked out)
        /// </summary>
        private async Task<ServiceResponse<List<AttendanceDto>>> GetAllActiveAttendancesAsync()
        {
            HttpResponseMessage response = null;

            try
            {
                response = await _httpClient.GetAsync($"{_baseUrl}/active");

                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonSerializer.Deserialize<ApiResponse>(jsonString, _jsonOptions);
                    if (apiResponse != null && apiResponse.Data != null)
                    {
                        try
                        {
                            var resultJson = apiResponse.Data.ToString();
                            var result = JsonSerializer.Deserialize<List<AttendanceDto>>(resultJson, _jsonOptions);

                            // Ensure all attendances have proper checkout time logic applied
                            if (result != null)
                            {
                                foreach (var attendance in result)
                                {
                                    attendance.EnsureCheckOutTime();
                                }
                            }

                            return new ServiceResponse<List<AttendanceDto>>(
                                true,
                                null,
                                result ?? new List<AttendanceDto>());
                        }
                        catch (JsonException je)
                        {
                            Console.WriteLine($"JSON parsing error: {je.Message}");
                            return new ServiceResponse<List<AttendanceDto>>(
                                false,
                                $"Data parsing error: {je.Message}",
                                null);
                        }
                    }

                    return new ServiceResponse<List<AttendanceDto>>(
                        true,
                        null,
                        new List<AttendanceDto>());
                }

                var errorMessage = await response.Content.ReadAsStringAsync();
                return new ServiceResponse<List<AttendanceDto>>(
                    false,
                    $"Failed to get active attendances: {errorMessage}",
                    null);
            }
            catch (HttpRequestException hre)
            {
                Console.WriteLine($"HTTP Request error: {hre.Message}");
                return new ServiceResponse<List<AttendanceDto>>(
                    false,
                    $"Network error: {hre.Message}",
                    null);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting active attendances: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return new ServiceResponse<List<AttendanceDto>>(
                    false,
                    $"Error getting active attendances: {ex.Message}",
                    null);
            }
            finally
            {
                response?.Dispose();
            }
        }

        public async Task<ServiceResponse<PagedResponse<AttendanceDto>>> GetAllAttendanceByDateRangeAsync(DateTime fromDate, DateTime toDate, int page = 1, int pageSize = 50)
        {
            try
            {
                Console.WriteLine($"Getting all attendance records from {fromDate:yyyy-MM-dd} to {toDate:yyyy-MM-dd}, page={page}, pageSize={pageSize}");

                var apiUrl = $"{_baseUrl}?fromDate={fromDate:yyyy-MM-dd}&toDate={toDate:yyyy-MM-dd}&page={page}&pageSize={pageSize}";
                Console.WriteLine($"Calling API: GET {apiUrl}");

                var response = await _httpClient.GetAsync(apiUrl);
                Console.WriteLine($"GetAllAttendanceByDateRangeAsync API response: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Raw API response: {content}");

                    try
                    {
                        var result = JsonSerializer.Deserialize<ApiResponse>(content, _jsonOptions);

                        if (result != null && result.Success && result.Data != null)
                        {
                            try
                            {
                                var resultJson = result.Data.ToString();
                                var attendanceData = JsonSerializer.Deserialize<PagedResponse<AttendanceDto>>(resultJson, _jsonOptions);

                                if (attendanceData != null)
                                {
                                    // Ensure all attendances have proper checkout time logic applied
                                    foreach (var attendance in attendanceData.Items)
                                    {
                                        attendance.EnsureCheckOutTime();
                                    }

                                    return new ServiceResponse<PagedResponse<AttendanceDto>>
                                    {
                                        Success = true,
                                        Data = attendanceData
                                    };
                                }
                            }
                            catch (JsonException je)
                            {
                                Console.WriteLine($"JSON parsing error: {je.Message}");
                                return new ServiceResponse<PagedResponse<AttendanceDto>>
                                {
                                    Success = false,
                                    Message = $"Data parsing error: {je.Message}"
                                };
                            }
                        }

                        return new ServiceResponse<PagedResponse<AttendanceDto>>
                        {
                            Success = result?.Success ?? false,
                            Message = result?.Message ?? "Failed to parse API response"
                        };
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error parsing API response: {ex.Message}");
                        return new ServiceResponse<PagedResponse<AttendanceDto>>
                        {
                            Success = false,
                            Message = $"Error parsing API response: {ex.Message}"
                        };
                    }
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"API error: {errorContent}");

                return new ServiceResponse<PagedResponse<AttendanceDto>>
                {
                    Success = false,
                    Message = $"Failed to get attendance records: {response.StatusCode}. Details: {errorContent}"
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in GetAllAttendanceByDateRangeAsync: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");

                return new ServiceResponse<PagedResponse<AttendanceDto>>
                {
                    Success = false,
                    Message = $"Error retrieving attendance: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Gets the attendance for a specific user by date range
        /// </summary>
        public async Task<ServiceResponse<List<AttendanceDto>>> GetAttendanceByUserIdAsync(string userId, DateTime startDate, DateTime endDate)
        {
            if (string.IsNullOrEmpty(userId))
            {
                Console.WriteLine("GetAttendanceByUserIdAsync error: User ID is empty");
                return new ServiceResponse<List<AttendanceDto>>
                {
                    Success = false,
                    Message = "User ID cannot be empty"
                };
            }

            HttpResponseMessage response = null;

            try
            {
                // Format dates for query parameters
                var formattedStartDate = startDate.ToString("yyyy-MM-dd");
                var formattedEndDate = endDate.ToString("yyyy-MM-dd");

                var apiUrl = $"{_baseUrl}/user/{userId}?startDate={formattedStartDate}&endDate={formattedEndDate}";
                Console.WriteLine($"Calling API: GET {apiUrl}");

                response = await _httpClient.GetAsync(apiUrl);
                Console.WriteLine($"Get attendance by user ID API call response: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Raw response: {responseContent}");

                    try
                    {
                        var apiResponse = JsonSerializer.Deserialize<ApiResponse>(responseContent, _jsonOptions);

                        if (apiResponse != null && apiResponse.Success && apiResponse.Data != null)
                        {
                            try
                            {
                                var resultJson = apiResponse.Data.ToString();
                                Console.WriteLine($"PagedResponse<AttendanceDto> data JSON: {resultJson}");

                                // First, deserialize to PagedResponse
                                var pagedResponse = JsonSerializer.Deserialize<PagedResponse<AttendanceDto>>(resultJson, _jsonOptions);
                                if (pagedResponse != null && pagedResponse.Items != null)
                                {
                                    // Return the list of items from the paged response
                                    return new ServiceResponse<List<AttendanceDto>>
                                    {
                                        Success = true,
                                        Data = pagedResponse.Items
                                    };
                                }
                            }
                            catch (JsonException je)
                            {
                                Console.WriteLine($"JSON parsing error: {je.Message}");
                                return new ServiceResponse<List<AttendanceDto>>
                                {
                                    Success = false,
                                    Message = $"Data parsing error: {je.Message}"
                                };
                            }
                        }

                        return new ServiceResponse<List<AttendanceDto>>
                        {
                            Success = apiResponse?.Success ?? false,
                            Message = apiResponse?.Message ?? "API call failed"
                        };
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error parsing API response: {ex.Message}");
                        return new ServiceResponse<List<AttendanceDto>>
                        {
                            Success = false,
                            Message = $"Error parsing API response: {ex.Message}"
                        };
                    }
                }

                var errorMessage = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Get attendance by user ID API call failed: {response.StatusCode}, Error: {errorMessage}");
                return new ServiceResponse<List<AttendanceDto>>
                {
                    Success = false,
                    Message = $"Failed to get attendance: {errorMessage}"
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during get attendance by user ID: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return new ServiceResponse<List<AttendanceDto>>
                {
                    Success = false,
                    Message = $"Error during get attendance: {ex.Message}"
                };
            }
            finally
            {
                response?.Dispose();
            }
        }

        /// <summary>
        /// Gets detailed attendance information with various filtering options
        /// </summary>
        public async Task<ServiceResponse<PagedResponse<DetailedAttendanceDto>>> GetDetailedAttendanceAsync(
            string userId = null,
            string attendanceId = null,
            string employeeName = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            int page = 1,
            int pageSize = 10)
        {
            try
            {
                await SetupHttpClientAsync();

                // Build query string with parameters
                var queryParams = new List<string>();

                if (!string.IsNullOrEmpty(userId))
                    queryParams.Add($"userId={Uri.EscapeDataString(userId)}");

                if (!string.IsNullOrEmpty(attendanceId))
                    queryParams.Add($"attendanceId={Uri.EscapeDataString(attendanceId)}");

                if (!string.IsNullOrEmpty(employeeName))
                    queryParams.Add($"employeeName={Uri.EscapeDataString(employeeName)}");

                if (startDate.HasValue)
                    queryParams.Add($"startDate={startDate.Value.ToString("yyyy-MM-ddTHH:mm:ss")}");

                if (endDate.HasValue)
                    queryParams.Add($"endDate={endDate.Value.ToString("yyyy-MM-ddTHH:mm:ss")}");

                queryParams.Add($"page={page}");
                queryParams.Add($"pageSize={pageSize}");

                string queryString = string.Join("&", queryParams);

                // Construct the URL
                string apiUrl = $"{_baseUrl}/detailed?{queryString}";

                // Log the request for debugging
                Console.WriteLine($"GetDetailedAttendanceAsync - Requesting: {apiUrl}");

                var response = await _httpClient.GetAsync(apiUrl);

                // Always read the response content for error details
                string responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"GetDetailedAttendanceAsync - Response status: {response.StatusCode}");
                Console.WriteLine($"GetDetailedAttendanceAsync - Response content: {responseContent}");

                if (response.IsSuccessStatusCode)
                {
                    // Deserialize successful response
                    var apiResponse = JsonSerializer.Deserialize<ApiResponse<PagedResponse<DetailedAttendanceDto>>>(
                        responseContent, _jsonOptions);

                    if (apiResponse == null)
                    {
                        return new ServiceResponse<PagedResponse<DetailedAttendanceDto>>(
                            false, "Failed to deserialize API response", null);
                    }

                    return new ServiceResponse<PagedResponse<DetailedAttendanceDto>>(
                        apiResponse.Success,
                        apiResponse.Message,
                        apiResponse.Data);
                }
                else
                {
                    // Try to extract error message from response content
                    string errorMessage = "API request failed";

                    try
                    {
                        // Attempt to deserialize error response
                        var errorResponse = JsonSerializer.Deserialize<ApiResponse>(responseContent, _jsonOptions);
                        if (errorResponse != null && !string.IsNullOrEmpty(errorResponse.Message))
                        {
                            errorMessage = errorResponse.Message;
                        }
                    }
                    catch
                    {
                        // If we can't deserialize the error, use the status code
                        errorMessage = $"API request failed with status code {(int)response.StatusCode}: {response.ReasonPhrase}";
                    }

                    return new ServiceResponse<PagedResponse<DetailedAttendanceDto>>(
                        false, errorMessage, null);
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"HTTP request error in GetDetailedAttendanceAsync: {ex.Message}");
                return new ServiceResponse<PagedResponse<DetailedAttendanceDto>>(
                    false, $"Error connecting to attendance API: {ex.Message}", null);
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"JSON deserialization error in GetDetailedAttendanceAsync: {ex.Message}");
                return new ServiceResponse<PagedResponse<DetailedAttendanceDto>>(
                    false, $"Error processing attendance data: {ex.Message}", null);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error in GetDetailedAttendanceAsync: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return new ServiceResponse<PagedResponse<DetailedAttendanceDto>>(
                    false, $"Unexpected error: {ex.Message}", null);
            }
        }

        public async Task<ServiceResponse<CheckInCheckOutStatusDto>> GetCheckInCheckOutStatusAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                Console.WriteLine("GetCheckInCheckOutStatusAsync error: User ID is empty");
                return new ServiceResponse<CheckInCheckOutStatusDto>
                {
                    Success = false,
                    Message = "User ID cannot be empty"
                };
            }

            try
            {
                await SetupHttpClientAsync();

                var apiUrl = $"{_baseUrl}/{userId}/checkin-checkout-status";
                Console.WriteLine($"Calling API: GET {apiUrl}");

                var response = await _httpClient.GetAsync(apiUrl);
                Console.WriteLine($"Get check-in/check-out status API call response: {response.StatusCode}");

                var responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"GetCheckInCheckOutStatusAsync raw response: {responseContent}");

                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        var apiResponse = JsonSerializer.Deserialize<ApiResponse<CheckInCheckOutStatusDto>>(
                            responseContent,
                            _jsonOptions
                        );

                        if (apiResponse == null)
                        {
                            Console.WriteLine("Failed to deserialize API response - null result");
                            return new ServiceResponse<CheckInCheckOutStatusDto>
                            {
                                Success = false,
                                Message = "Failed to parse API response"
                            };
                        }

                        if (apiResponse.Data == null)
                        {
                            Console.WriteLine("API response data is null");
                            return new ServiceResponse<CheckInCheckOutStatusDto>
                            {
                                Success = apiResponse.Success,
                                Message = apiResponse.Message ?? "No status data returned from API"
                            };
                        }

                        Console.WriteLine($"Status parsed successfully: IsCheckedIn={apiResponse.Data.IsCheckedIn}, UserId={apiResponse.Data.UserId}, AttendanceId={apiResponse.Data.CurrentAttendanceId}");

                        return new ServiceResponse<CheckInCheckOutStatusDto>
                        {
                            Success = apiResponse.Success,
                            Message = apiResponse.Message,
                            Data = apiResponse.Data
                        };
                    }
                    catch (JsonException jsonEx)
                    {
                        Console.WriteLine($"JSON parsing error: {jsonEx.Message}");
                        Console.WriteLine($"JSON content: {responseContent}");
                        return new ServiceResponse<CheckInCheckOutStatusDto>
                        {
                            Success = false,
                            Message = $"JSON parsing error: {jsonEx.Message}"
                        };
                    }
                }
                else
                {
                    Console.WriteLine($"GetCheckInCheckOutStatusAsync error: {response.StatusCode}");

                    // Try to extract error message from response content
                    string errorMessage = $"API error: {response.StatusCode}";

                    try
                    {
                        var errorResponse = JsonSerializer.Deserialize<ApiResponse>(responseContent, _jsonOptions);
                        if (errorResponse != null && !string.IsNullOrEmpty(errorResponse.Message))
                        {
                            errorMessage = errorResponse.Message;
                        }
                    }
                    catch
                    {
                        // If we can't parse the error response, use the default message
                    }

                    return new ServiceResponse<CheckInCheckOutStatusDto>
                    {
                        Success = false,
                        Message = errorMessage
                    };
                }
            }
            catch (HttpRequestException httpEx)
            {
                Console.WriteLine($"HTTP request exception: {httpEx.Message}");
                return new ServiceResponse<CheckInCheckOutStatusDto>
                {
                    Success = false,
                    Message = $"Network error: {httpEx.Message}"
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetCheckInCheckOutStatusAsync exception: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return new ServiceResponse<CheckInCheckOutStatusDto>
                {
                    Success = false,
                    Message = $"Error: {ex.Message}"
                };
            }
        }

        // Method to get today's attendance statistics
        public async Task<ServiceResponse<AttendanceStatsDto>> GetTodayAttendanceStatsAsync()
        {
            try
            {
                await SetupHttpClientAsync();

                string apiUrl = $"{_baseUrl}/today-stats";
                Console.WriteLine($"Calling API: GET {apiUrl}");

                HttpResponseMessage response = await _httpClient.GetAsync(apiUrl);
                string responseContent = await response.Content.ReadAsStringAsync();

                Console.WriteLine($"GetTodayAttendanceStatsAsync - Response status: {response.StatusCode}");
                Console.WriteLine($"GetTodayAttendanceStatsAsync - Response content: {responseContent}");

                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        var apiResponse = JsonSerializer.Deserialize<ApiResponse<AttendanceStatsDto>>(
                            responseContent,
                            _jsonOptions
                        );

                        if (apiResponse == null)
                        {
                            Console.WriteLine("Failed to deserialize attendance stats API response - null result");
                            return new ServiceResponse<AttendanceStatsDto>
                            {
                                Success = false,
                                Message = "Failed to parse attendance statistics"
                            };
                        }

                        if (apiResponse.Data == null)
                        {
                            Console.WriteLine("API attendance stats response data is null");
                            return new ServiceResponse<AttendanceStatsDto>
                            {
                                Success = apiResponse.Success,
                                Message = apiResponse.Message ?? "No attendance statistics data returned from API"
                            };
                        }

                        Console.WriteLine($"Attendance stats parsed successfully. Total: {apiResponse.Data.TotalEmployees}, Arrived: {apiResponse.Data.TotalArrived}, OnTime: {apiResponse.Data.OnTime}, Late: {apiResponse.Data.LateArrivals}");

                        return new ServiceResponse<AttendanceStatsDto>
                        {
                            Success = apiResponse.Success,
                            Message = apiResponse.Message,
                            Data = apiResponse.Data
                        };
                    }
                    catch (JsonException jsonEx)
                    {
                        Console.WriteLine($"JSON parsing error: {jsonEx.Message}");
                        Console.WriteLine($"JSON content: {responseContent}");
                        return new ServiceResponse<AttendanceStatsDto>
                        {
                            Success = false,
                            Message = $"JSON parsing error: {jsonEx.Message}"
                        };
                    }
                }
                else
                {
                    Console.WriteLine($"GetTodayAttendanceStatsAsync error: {response.StatusCode}");

                    // Try to extract error message from response content
                    string errorMessage = "Failed to retrieve attendance statistics";

                    try
                    {
                        var errorResponse = JsonSerializer.Deserialize<ApiResponse>(responseContent, _jsonOptions);
                        if (errorResponse != null && !string.IsNullOrEmpty(errorResponse.Message))
                        {
                            errorMessage = errorResponse.Message;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error parsing error response: {ex.Message}");
                    }

                    return new ServiceResponse<AttendanceStatsDto>
                    {
                        Success = false,
                        Message = $"{errorMessage}. Status code: {(int)response.StatusCode}"
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in GetTodayAttendanceStatsAsync: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");

                return new ServiceResponse<AttendanceStatsDto>
                {
                    Success = false,
                    Message = $"Error retrieving attendance statistics: {ex.Message}"
                };
            }
        }
    }

    public class ServiceResponse<T>
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public T? Data { get; set; }

        // Parameterless constructor to support property initialization
        public ServiceResponse()
        {
        }

        public ServiceResponse(bool success, string? message, T? data)
        {
            Success = success;
            Message = message;
            Data = data;
        }
    }

    public class ApiResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public object? Data { get; set; }
    }
}