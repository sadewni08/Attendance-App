using ChronoTrack_ViewLayer.Services;
using ChronoTrack_ViewLayer.Pages;
using ChronoTrack_ViewLayer.Pages.User;
using Microsoft.Identity.Client;
using System.IdentityModel.Tokens.Jwt;

namespace ChronoTrack_ViewLayer
{
    public partial class MainPage : ContentPage
    {
        private readonly AuthenticationService _authService;
        private readonly EmployeeService _employeeService;
        private bool _isDesktopView;
        private const double DesktopThresholdWidth = 800;
        
        // Microsoft Authentication settings
        private readonly string _clientId = "12345678-1234-1234-1234-123456789012"; // Sample client ID - replace with your actual Azure AD app client ID
        private readonly string _tenantId = "common"; // Use "common" for multi-tenant or your specific tenant ID
        private readonly string[] _scopes = { "User.Read", "email", "profile" };
        private IPublicClientApplication _msalClient;

        public MainPage()
        {
            InitializeComponent();
            _authService = new AuthenticationService();
            _employeeService = new EmployeeService();
            
            InitializeMicrosoftAuth();
            SetupEventHandlers();
            
            // Initial layout
            HandleResponsiveLayout(Width);
        }
        
        private void InitializeMicrosoftAuth()
        {
            try
            {
                // Validate client ID is not the placeholder
                if (_clientId == "YOUR_AZURE_AD_CLIENT_ID" || _clientId == "12345678-1234-1234-1234-123456789012")
                {
                    Console.WriteLine("WARNING: You need to replace the placeholder client ID with your actual Azure AD app client ID");
                }

                // Get OS-specific redirect URI
                string redirectUri = GetRedirectUri();
                Console.WriteLine($"Using redirect URI: {redirectUri}");
                
                // Create MSAL client
                _msalClient = PublicClientApplicationBuilder
                    .Create(_clientId)
                    .WithRedirectUri(redirectUri)
                    .WithAuthority(AzureCloudInstance.AzurePublic, _tenantId)
                    .Build();
                
                Console.WriteLine("Microsoft Authentication initialized successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing Microsoft Authentication: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                _msalClient = null; // Ensure it's null so we can check later
            }
        }
        
        private string GetRedirectUri()
        {
            // Get platform-specific redirect URI
            string redirectUri;
            
#if ANDROID
            redirectUri = $"msal{_clientId}://auth";
#elif IOS
            redirectUri = $"msal{_clientId}://auth";
#else // Windows and others
            redirectUri = "http://localhost";
#endif
            
            return redirectUri;
        }

        /// <summary>
        /// Set up event handlers for interactive elements
        /// </summary>
        private void SetupEventHandlers()
        {
            // Mobile view event handlers
            SignInButtonMobile.Clicked += OnSignInClicked;
            ForgotPasswordTappedMobile.Tapped += OnForgotPasswordTapped;
            
            // Desktop view event handlers
            SignInButtonDesktop.Clicked += OnSignInClicked;
            ForgotPasswordTappedDesktop.Tapped += OnForgotPasswordTapped;
            
            // Setup two-way binding between mobile and desktop inputs
            EmailEntryMobile.TextChanged += (s, e) => EmailEntryDesktop.Text = EmailEntryMobile.Text;
            EmailEntryDesktop.TextChanged += (s, e) => EmailEntryMobile.Text = EmailEntryDesktop.Text;
            
            PasswordEntryMobile.TextChanged += (s, e) => PasswordEntryDesktop.Text = PasswordEntryMobile.Text;
            PasswordEntryDesktop.TextChanged += (s, e) => PasswordEntryMobile.Text = PasswordEntryDesktop.Text;
        }
        
        /// <summary>
        /// Handle page size changes to switch between mobile and desktop views
        /// </summary>
        private void OnPageSizeChanged(object sender, EventArgs e)
        {
            var width = Application.Current?.MainPage?.Width ?? 0;
            _isDesktopView = width >= DesktopThresholdWidth;

            MobileView.IsVisible = !_isDesktopView;
            DesktopView.IsVisible = _isDesktopView;
        }
        
        /// <summary>
        /// Switch between mobile and desktop layouts based on screen width
        /// </summary>
        private void HandleResponsiveLayout(double width)
        {
            bool isDesktop = width >= DesktopThresholdWidth;
            
            MobileView.IsVisible = !isDesktop;
            DesktopView.IsVisible = isDesktop;
        }

        /// <summary>
        /// Handle sign in button click
        /// </summary>
        private async void OnSignInClicked(object sender, EventArgs e)
        {
            try
            {
                var email = _isDesktopView ? EmailEntryDesktop.Text : EmailEntryMobile.Text;
                var password = _isDesktopView ? PasswordEntryDesktop.Text : PasswordEntryMobile.Text;

                if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                {
                    await DisplayAlert("Error", "Please enter both email and password", "OK");
                    return;
                }

                // Show loading indicator
                IsBusy = true;

                var response = await _authService.LoginAsync(email, password);

                if (response != null)
                {
                    // Store user info in app preferences
                    Preferences.Set("UserToken", response.Token);
                    Preferences.Set("UserRole", response.Role);
                    Preferences.Set("UserName", response.FullName);
                    Preferences.Set("UserEmail", response.EmailAddress);
                    Preferences.Set("UserId", response.UserId);

                    // Navigate to appropriate dashboard based on role
                    NavigateBasedOnRole(response.Role);
                }
                else
                {
                    await DisplayAlert("Error", "Invalid email or password", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "An error occurred while signing in", "OK");
                Console.WriteLine($"Sign in error: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void NavigateBasedOnRole(string role)
        {
            if (role.Equals("Administrator", StringComparison.OrdinalIgnoreCase) || 
                role.Equals("Manager", StringComparison.OrdinalIgnoreCase))
            {
                Navigation.PushAsync(new DashboardPage());
            }
            else
            {
                Navigation.PushAsync(new UserDashboardPage());
            }
        }

        /// <summary>
        /// Handle forgot password link tap
        /// </summary>
        private async void OnForgotPasswordTapped(object sender, TappedEventArgs e)
        {
            // TODO: Implement forgot password logic
            await DisplayAlert("Forgot Password", "Reset password functionality will be implemented", "OK");
        }

        /// <summary>
        /// Handle Microsoft sign in button click
        /// </summary>
        private async void OnMicrosoftSignInClicked(object sender, EventArgs e)
        {
            try
            {
                // Show loading indicator
                IsBusy = true;
                
                // Check if MSAL client is initialized
                if (_msalClient == null)
                {
                    await DisplayAlert("Error", "Microsoft authentication is not properly initialized", "OK");
                    return;
                }
                
                // Try to sign in with Microsoft
                var result = await SignInWithMicrosoftAsync();
                if (result.success)
                {
                    // Check if the email exists in the database
                    var isRegisteredUser = await _employeeService.IsUserRegisteredAsync(result.userInfo.Email);
                    
                    if (isRegisteredUser)
                    {
                        // Get user details from the employee service
                        var userDetails = await _employeeService.GetEmployeeByEmailAsync(result.userInfo.Email);
                        
                        if (userDetails != null)
                        {
                            // Store user info in app preferences
                            Preferences.Set("UserToken", result.userInfo.AccessToken);
                            Preferences.Set("UserRole", userDetails.Role ?? "User");
                            Preferences.Set("UserName", result.userInfo.Name);
                            Preferences.Set("UserEmail", result.userInfo.Email);
                            Preferences.Set("UserId", userDetails.UserId);
                            
                            // Navigate to appropriate dashboard based on role
                            NavigateBasedOnRole(userDetails.Role ?? "User");
                        }
                        else
                        {
                            await DisplayAlert("Authentication Failed", "Failed to retrieve user details", "OK");
                        }
                    }
                    else
                    {
                        // Email not registered in the system
                        await DisplayAlert("Access Denied", 
                            "Your Microsoft account email is not registered in the system. Please contact your administrator.", "OK");
                    }
                }
                else
                {
                    // Authentication failed
                    await DisplayAlert("Authentication Failed", result.message, "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Microsoft authentication error: {ex.Message}", "OK");
                Console.WriteLine($"Microsoft sign in error: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }
        
        private async Task<(bool success, string message, MicrosoftUserInfo userInfo)> SignInWithMicrosoftAsync()
        {
            try
            {
                // Try to acquire token silently first (if user previously authenticated)
                var accounts = await _msalClient.GetAccountsAsync();
                AuthenticationResult authResult = null;
                
                if (accounts.Any())
                {
                    try
                    {
                        authResult = await _msalClient.AcquireTokenSilent(_scopes, accounts.FirstOrDefault())
                            .ExecuteAsync();
                    }
                    catch (MsalUiRequiredException)
                    {
                        // Silent authentication failed, user interaction required
                    }
                }
                
                // If silent auth failed, prompt user for interactive sign in
                if (authResult == null)
                {
                    authResult = await _msalClient.AcquireTokenInteractive(_scopes)
                        .WithPrompt(Prompt.SelectAccount)
                        .ExecuteAsync();
                }
                
                // Parse the token
                var handler = new JwtSecurityTokenHandler();
                var idToken = handler.ReadJwtToken(authResult.IdToken);
                
                // Extract user info
                var name = idToken.Claims.FirstOrDefault(c => c.Type == "name")?.Value ?? string.Empty;
                var email = idToken.Claims.FirstOrDefault(c => c.Type == "preferred_username")?.Value ?? string.Empty;
                
                // Create user info object
                var userInfo = new MicrosoftUserInfo
                {
                    Name = name,
                    Email = email,
                    AccessToken = authResult.AccessToken,
                    IdToken = authResult.IdToken
                };
                
                return (true, "Authentication successful", userInfo);
            }
            catch (MsalClientException ex)
            {
                return (false, $"MSAL client error: {ex.Message}", null);
            }
            catch (MsalServiceException ex)
            {
                return (false, $"MSAL service error: {ex.Message}", null);
            }
            catch (Exception ex)
            {
                return (false, $"Authentication error: {ex.Message}", null);
            }
        }
    }
    
    public class MicrosoftUserInfo
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public string AccessToken { get; set; }
        public string IdToken { get; set; }
    }
}
