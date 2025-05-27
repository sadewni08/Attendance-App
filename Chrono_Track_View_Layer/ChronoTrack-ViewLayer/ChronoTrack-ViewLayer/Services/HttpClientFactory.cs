using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace ChronoTrack_ViewLayer.Services
{
    /// <summary>
    /// Factory class for creating and managing HttpClient instances
    /// </summary>
    public static class HttpClientFactory
    {
        private static readonly Lazy<HttpClient> _clientInstance = new Lazy<HttpClient>(() => CreateClient());
        private const string BaseUrl = "https://localhost:7222/";
        
        /// <summary>
        /// Number of retry attempts for transient errors
        /// </summary>
        private const int MaxRetries = 3;

        /// <summary>
        /// Get the singleton HttpClient instance
        /// </summary>
        public static HttpClient Client => _clientInstance.Value;
        
        /// <summary>
        /// Execute an HTTP request with retry logic for transient errors
        /// </summary>
        public static async Task<HttpResponseMessage> ExecuteWithRetry(Func<Task<HttpResponseMessage>> requestFunc)
        {
            int retryCount = 0;
            TimeSpan delay = TimeSpan.FromSeconds(1);

            while (true)
            {
                try
                {
                    return await requestFunc();
                }
                catch (Exception ex) when (
                    (ex is HttpRequestException || ex is SocketException || ex is TaskCanceledException) &&
                    retryCount < MaxRetries)
                {
                    retryCount++;
                    if (retryCount >= MaxRetries)
                        throw;

                    Console.WriteLine($"Request failed with {ex.GetType().Name}: {ex.Message}. Retrying ({retryCount}/{MaxRetries})...");
                    
                    // Add exponential backoff
                    await Task.Delay(delay);
                    delay = TimeSpan.FromSeconds(Math.Min(30, Math.Pow(2, retryCount)));
                }
            }
        }

        /// <summary>
        /// Create a properly configured HttpClient instance
        /// </summary>
        private static HttpClient CreateClient()
        {
            // Make sure the URL ends with a "/" for proper path resolution
            string baseUrl = BaseUrl;
            if (!baseUrl.EndsWith("/"))
            {
                baseUrl += "/";
            }
            
            var handler = new HttpClientHandler
            {
                // For development only - allows using self-signed SSL certs
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };
            
            var client = new HttpClient(handler)
            {
                BaseAddress = new Uri(baseUrl),
                Timeout = TimeSpan.FromMinutes(2)
            };
            
            // Set default headers
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            
            return client;
        }
    }
} 