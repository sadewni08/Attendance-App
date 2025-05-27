using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using ChronoTrack_ViewLayer.Models;

namespace ChronoTrack_ViewLayer.Services
{
    public class AuthenticationService
    {
        private readonly HttpClient _httpClient;
        private const string BaseUrl = "https://localhost:7222/"; // Update this with your backend API URL

        public AuthenticationService()
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(BaseUrl)
            };
        }

        public async Task<UserAuthResponseDto?> LoginAsync(string email, string password)
        {
            try
            {
                var loginDto = new UserLoginDto(email, password);
                var response = await _httpClient.PostAsJsonAsync("/api/auth/login", loginDto);

                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var result = JsonSerializer.Deserialize<ApiResponse<UserAuthResponseDto>>(jsonString, jsonOptions);
                    return result?.Data;
                }

                return null;
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine($"Login failed: {ex.Message}");
                return null;
            }
        }
    }

    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
    }
} 