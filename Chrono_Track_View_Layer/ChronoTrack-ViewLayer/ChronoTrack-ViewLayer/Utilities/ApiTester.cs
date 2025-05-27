using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ChronoTrack_ViewLayer.Models;

namespace ChronoTrack_ViewLayer.Utilities
{
    public static class ApiTester
    {
        private static readonly HttpClient _httpClient = new HttpClient 
        { 
            BaseAddress = new Uri("https://localhost:7222/"),
            Timeout = TimeSpan.FromMinutes(1)
        };

        /// <summary>
        /// Tests the employee update endpoint with direct HTTP request
        /// </summary>
        public static async Task<string> TestUpdateEmployeeEndpoint(Guid employeeId, UpdateEmployeeDto updateDto)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true
                };
                
                // Create a dictionary with the exact properties needed and specific casing
                var employeeDict = new Dictionary<string, object>
                {
                    { "userId", updateDto.UserId },
                    { "firstName", updateDto.FirstName },
                    { "lastName", updateDto.LastName },
                    { "gender", updateDto.Gender },
                    { "address", updateDto.Address },
                    { "emailAddress", updateDto.EmailAddress },
                    { "phoneNumber", updateDto.PhoneNumber },
                    { "departmentID", updateDto.DepartmentID }, // Use correct case
                    { "userRoleID", updateDto.UserRoleID }     // Use correct case
                };
                
                string json = JsonSerializer.Serialize(employeeDict, options);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                // Print the request info for debugging
                string requestInfo = $"PUT: https://localhost:7222/api/employees/{employeeId}\n" +
                                    $"Headers: Content-Type: application/json\n" +
                                    $"Body: {json}";
                
                Console.WriteLine(requestInfo);
                
                // Make direct PUT request to the API
                var response = await _httpClient.PutAsync($"/api/employees/{employeeId}", content);
                
                string responseContent = await response.Content.ReadAsStringAsync();
                
                return $"Status: {response.StatusCode}\n" +
                       $"Response: {responseContent}";
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }

        /// <summary>
        /// Generates a curl command that can be used to test the API manually
        /// </summary>
        public static string GenerateUpdateEmployeeCurlCommand(Guid employeeId, UpdateEmployeeDto updateDto)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };
            
            // Create a dictionary with the exact properties needed and specific casing
            var employeeDict = new Dictionary<string, object>
            {
                { "userId", updateDto.UserId },
                { "firstName", updateDto.FirstName },
                { "lastName", updateDto.LastName },
                { "gender", updateDto.Gender },
                { "address", updateDto.Address },
                { "emailAddress", updateDto.EmailAddress },
                { "phoneNumber", updateDto.PhoneNumber },
                { "departmentID", updateDto.DepartmentID }, // Use correct case
                { "userRoleID", updateDto.UserRoleID }     // Use correct case
            };
            
            string json = JsonSerializer.Serialize(employeeDict, options);
            
            // Format the curl command with the -k flag to ignore SSL certificate validation
            return $"curl -X PUT https://localhost:7222/api/employees/{employeeId} " +
                   $"-H \"Content-Type: application/json\" " +
                   $"-d \"{json.Replace("\"", "\\\"")}\" -k";
        }
    }
}