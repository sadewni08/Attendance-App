using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using System;
using System.Threading.Tasks;

namespace ChronoTrack_ViewLayer.Components
{
    public partial class MobileHeaderComponent : ContentView
    {
        public event EventHandler MenuClicked;
        public event EventHandler ProfileClicked;
        public event EventHandler CalendarClicked;
        public event EventHandler SearchToggled;
        public event EventHandler<string> NavigationRequested;
        public event EventHandler LogoutRequested;

        public Command CalendarCommand { get; private set; }

        // A reference to the profile popup (added at runtime if needed)
        private Border _profilePopup;

        public MobileHeaderComponent()
        {
            InitializeComponent();
            SetupCommands();
            InitializeProfilePopup();

            // Wire up menu popup events
            if (MenuPopup != null)
            {
                MenuPopup.MenuItemClicked += OnMenuItemClicked;
                MenuPopup.CloseRequested += OnMenuCloseRequested;
            }
        }

        private void SetupCommands()
        {
            CalendarCommand = new Command(() =>
            {
                CalendarClicked?.Invoke(this, EventArgs.Empty);
            });

            BindingContext = this;
        }

        private void InitializeProfilePopup()
        {
            // Create a popup for profile actions
            _profilePopup = new Border
            {
                IsVisible = false,
                StrokeShape = new RoundRectangle() { CornerRadius = new CornerRadius(8) },
                Stroke = new SolidColorBrush(Color.FromArgb("#E5E7EB")),
                StrokeThickness = 1,
                BackgroundColor = Colors.White,
                VerticalOptions = LayoutOptions.Start,
                HorizontalOptions = LayoutOptions.End,
                Margin = new Thickness(0, 80, 16, 0),
                Padding = new Thickness(0),
                ZIndex = 10000,
                Content = CreateProfileMenu()
            };

            // Add the popup to the grid
            Grid grid = this.Content as Grid;
            if (grid != null)
            {
                grid.Children.Add(_profilePopup);
            }
        }

        private VerticalStackLayout CreateProfileMenu()
        {
            var layout = new VerticalStackLayout { Spacing = 0 };
            
            // Add user info section
            var userInfoSection = new VerticalStackLayout
            {
                Padding = new Thickness(16),
                BackgroundColor = Color.FromArgb("#F3F4F6")
            };
            
            userInfoSection.Add(new Label
            {
                Text = Preferences.Get("UserName", "User"),
                FontAttributes = FontAttributes.Bold,
                FontSize = 16
            });
            
            userInfoSection.Add(new Label
            {
                Text = Preferences.Get("UserEmail", "user@example.com"),
                TextColor = Color.FromArgb("#6B7280"),
                FontSize = 14
            });
            
            layout.Add(userInfoSection);
            
            // Add logout button
            var logoutButton = new Button
            {
                Text = "Logout",
                TextColor = Color.FromArgb("#DF4646"),
                BackgroundColor = Colors.Transparent,
                FontAttributes = FontAttributes.Bold,
                Padding = new Thickness(16, 12),
                HorizontalOptions = LayoutOptions.Fill,
                BorderWidth = 0,
                ImageSource = "logout.png"
            };
            
            logoutButton.Clicked += OnLogoutClicked;
            layout.Add(logoutButton);
            
            return layout;
        }

        private void OnMenuClicked(object sender, EventArgs e)
        {
            // Hide profile popup if visible
            if (_profilePopup != null)
                _profilePopup.IsVisible = false;
                
            // Ensure the menu popup is visible and on top
            MenuPopup.IsVisible = true;
            MenuPopup.ZIndex = 9999;
            MenuClicked?.Invoke(this, e);
        }

        private void OnProfileClicked(object sender, EventArgs e)
        {
            // Toggle profile popup
            if (_profilePopup != null)
            {
                // Ensure menu is closed
                if (MenuPopup.IsVisible)
                    MenuPopup.IsVisible = false;
                    
                _profilePopup.IsVisible = !_profilePopup.IsVisible;
            }
            
            ProfileClicked?.Invoke(this, e);
        }

        private void OnMenuItemClicked(object sender, string route)
        {
            if (MenuPopup != null)
            {
                MenuPopup.IsVisible = false;
                NavigationRequested?.Invoke(this, route);
            }
        }

        private void OnMenuCloseRequested(object sender, EventArgs e)
        {
            if (MenuPopup != null)
            {
                MenuPopup.IsVisible = false;
            }
        }
        
        private void OnLogoutClicked(object sender, EventArgs e)
        {
            // Hide the popup
            if (_profilePopup != null)
                _profilePopup.IsVisible = false;
                
            // Trigger logout event
            LogoutRequested?.Invoke(this, EventArgs.Empty);
        }

        public void ToggleSearchBar()
        {
            SearchBar.IsVisible = !SearchBar.IsVisible;
            SearchToggled?.Invoke(this, EventArgs.Empty);
        }

        public async Task CloseMenuAsync()
        {
            if (MenuPopup.IsVisible)
            {
                MenuPopup.IsVisible = false;
            }
            
            if (_profilePopup != null && _profilePopup.IsVisible)
            {
                _profilePopup.IsVisible = false;
            }
        }
    }
} 