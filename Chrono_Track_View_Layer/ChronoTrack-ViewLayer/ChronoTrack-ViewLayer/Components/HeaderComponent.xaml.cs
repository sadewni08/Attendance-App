using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using ChronoTrack_ViewLayer.Pages;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Layouts;
using System.Globalization;

namespace ChronoTrack_ViewLayer.Components
{
    public partial class HeaderComponent : ContentView
    {
        // Event to notify parent pages when calendar is clicked
        public event EventHandler CalendarClicked;
        
        // Event to notify parent pages when notifications are clicked
        public event EventHandler NotificationsClicked;
        
        // Event to notify parent pages when menu is clicked
        public event EventHandler MenuClicked;

        public event EventHandler ProfileClicked;
        public event EventHandler<string> NavigationRequested;

        private MobileHeaderComponent _mobileHeader;

        public ICommand CalendarCommand { get; private set; }
        public ICommand NotificationsCommand { get; private set; }

        public event EventHandler LogoutRequested;

        public HeaderComponent()
        {
            InitializeComponent();
            
            SetupCommands();
            SetupMobileHeader();
            SetupPopupComponents();
            
            // Set profile information based on logged in user
            SetUserProfile();
        }

        private void SetupCommands()
        {
            // Add direct debug logging
            Console.WriteLine("Setting up HeaderComponent commands");
            
            CalendarCommand = new Command(() => 
            {
                Console.WriteLine("CalendarCommand executed");
                CalendarClicked?.Invoke(this, EventArgs.Empty);
            });
            
            NotificationsCommand = new Command(() => 
            {
                Console.WriteLine("NotificationsCommand executed");
                NotificationsClicked?.Invoke(this, EventArgs.Empty);
            });
            
            BindingContext = this;
            
            // Set up tap event handlers after Loaded event
            Loaded += HeaderComponent_Loaded;
        }

        private void SetupMobileHeader()
        {
            _mobileHeader = this.FindByName<MobileHeaderComponent>("MobileHeader");

            if (_mobileHeader != null)
            {
                _mobileHeader.MenuClicked += (s, e) => MenuClicked?.Invoke(this, e);
                _mobileHeader.ProfileClicked += (s, e) => ProfileClicked?.Invoke(this, e);
                _mobileHeader.NavigationRequested += (s, route) => NavigationRequested?.Invoke(this, route);
                _mobileHeader.LogoutRequested += OnMobileLogoutRequested;
            }
        }
        
        private void SetupPopupComponents()
        {
            // Wire up calendar popup events
            if (CalendarPopupComponent != null)
            {
                CalendarPopupComponent.DateSelected += (s, date) => {
                    Console.WriteLine($"Date selected: {date:MM/dd/yyyy}");
                    CloseAllPopups();
                };
                
                CalendarPopupComponent.CloseRequested += (s, e) => {
                    CloseAllPopups();
                };
            }
            
            // Wire up notifications popup events
            if (NotificationsPopupComponent != null)
            {
                NotificationsPopupComponent.ViewAllRequested += (s, e) => {
                    CloseAllPopups();
                    // Instead of navigating to a separate page, we'll just show a message
                    // or implement alternative behavior without needing the NotificationsPage
                    try
                    {
                        Application.Current.MainPage.DisplayAlert("Notifications", 
                            "Viewing all notifications in the current view.", "OK");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error displaying alert: {ex.Message}");
                    }
                };
                
                NotificationsPopupComponent.CloseRequested += (s, e) => {
                    CloseAllPopups();
                };
            }
        }

        private void OnMobileLogoutRequested(object sender, EventArgs e)
        {
            PerformLogout();
        }

        private void HeaderComponent_Loaded(object sender, EventArgs e)
        {
            Console.WriteLine("HeaderComponent loaded");
            
            // Set up mobile header events again if needed
            if (_mobileHeader != null)
            {
                _mobileHeader.MenuClicked += (s, e) => MenuClicked?.Invoke(this, e);
                _mobileHeader.ProfileClicked += (s, e) => ProfileClicked?.Invoke(this, e);
            }
            
            // Find and set up calendar border tap gesture
            if (CalendarBorder != null)
            {
                Console.WriteLine("Setting up calendar border tap gesture");
                // Clear existing gestures to avoid duplicates
                CalendarBorder.GestureRecognizers.Clear();
                
                // Add new tap gesture - directly call the OnCalendarBorderTapped method
                var calendarTap = new TapGestureRecognizer();
                calendarTap.Tapped += OnCalendarBorderTapped;
                CalendarBorder.GestureRecognizers.Add(calendarTap);
            }
            
            // Find and set up notification border tap gesture
            if (NotificationBorder != null)
            {
                Console.WriteLine("Setting up notification border tap gesture");
                // Clear existing gestures to avoid duplicates
                NotificationBorder.GestureRecognizers.Clear();
                
                // Add new tap gesture - directly call the OnNotificationBorderTapped method
                var notificationTap = new TapGestureRecognizer();
                notificationTap.Tapped += OnNotificationBorderTapped;
                NotificationBorder.GestureRecognizers.Add(notificationTap);
            }
            
            // Find and set up profile border tap gesture
            if (ProfileBorder != null)
            {
                Console.WriteLine("Setting up profile border tap gesture");
                // Clear existing gestures to avoid duplicates
                ProfileBorder.GestureRecognizers.Clear();
                
                // Add new tap gesture - directly call the OnProfileTapped method
                var profileTap = new TapGestureRecognizer();
                profileTap.Tapped += OnProfileTapped;
                ProfileBorder.GestureRecognizers.Add(profileTap);
            }
        }

        public async Task CloseMenuAsync()
        {
            if (_mobileHeader != null)
            {
                await _mobileHeader.CloseMenuAsync();
            }
        }

        private void SetUserProfile()
        {
            try
            {
                // Get user info from preferences
                var userName = Preferences.Get("UserName", "User");
                var userEmail = Preferences.Get("UserEmail", userName + "@example.com");

                // Set the UI values
                if (UserNameLabel != null)
                    UserNameLabel.Text = userName;
                if (UserEmailLabel != null)
                    UserEmailLabel.Text = userEmail;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error setting user profile: {ex.Message}");
            }
        }

        private void OnCalendarTapped()
        {
            // Handle calendar icon tap
            Console.WriteLine("OnCalendarTapped method called");
            CalendarClicked?.Invoke(this, EventArgs.Empty);
        }

        private void OnNotificationsTapped()
        {
            // Handle notifications icon tap
            Console.WriteLine("OnNotificationsTapped method called");
            NotificationsClicked?.Invoke(this, EventArgs.Empty);
        }

        private void OnProfileTapped(object sender, TappedEventArgs e)
        {
            Console.WriteLine("*** Profile tapped - showing logout menu ***");
            
            try
            {
                // First close any other popups
                CloseAllPopups();
                
                // Show the profile menu popup
                if (ProfileMenuPopup != null)
                {
                    // Always show the popup, don't toggle
                    ProfileMenuPopup.IsVisible = true;
                    
                    // Show overlay to allow clicking outside to close
                    if (PopupOverlay != null)
                    {
                        PopupOverlay.IsVisible = true;
                        PopupOverlay.ZIndex = 1999; // Just below the profile menu
                    }
                    
                    // Make sure it's on top
                    ProfileMenuPopup.ZIndex = 2000;
                    
                    Console.WriteLine("Profile menu popup displayed");
                }
                else
                {
                    Console.WriteLine("Profile menu popup is null!");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error showing profile menu: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        private async void OnLogoutClicked(object sender, EventArgs e)
        {
            // Hide the popup
            if (ProfileMenuPopup != null)
            {
                ProfileMenuPopup.IsVisible = false;
            }
            
            await PerformLogout();
        }

        private async Task PerformLogout()
        {
            bool confirm = await Application.Current.MainPage.DisplayAlert(
                "Logout", 
                "Are you sure you want to logout?", 
                "Yes", "No");
                
            if (!confirm)
                return;
                
            // Clear user preferences/session data
            Preferences.Remove("UserToken");
            Preferences.Remove("UserRole");
            Preferences.Remove("UserName");
            Preferences.Remove("UserEmail");
            
            // Raise event for parent page to handle logout navigation
            LogoutRequested?.Invoke(this, EventArgs.Empty);
            
            // If no parent is handling the event, navigate directly
            try
            {
                if (Application.Current?.MainPage != null)
                {
                    // Reset navigation stack and go to login page
                    Application.Current.MainPage = new NavigationPage(new MainPage());
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", 
                    $"Could not navigate to login page: {ex.Message}", "OK");
            }
        }

        private void OnCalendarBorderTapped(object sender, TappedEventArgs e)
        {
            Console.WriteLine("*** Calendar border tapped from XAML handler ***");
            
            try
            {
                // First, close all popups to start fresh
                CloseAllPopups();
                
                // Now, show calendar popup
                if (CalendarPopupComponent != null)
                {
                    // Show calendar popup
                    CalendarPopupComponent.IsVisible = true;
                    
                    // Refresh calendar
                    CalendarPopupComponent.RefreshCalendar();
                    
                    // Show overlay
                    PopupOverlay.IsVisible = true;
                    
                    // Ensure proper z-index
                    CalendarPopupComponent.ZIndex = 99999;
                    PopupOverlay.ZIndex = 99998;
                    
                    // Force popups to appear above everything else
                    EnsurePopupsAreOnTop();
                    
                    Console.WriteLine("Calendar popup opened successfully");
                }
                else
                {
                    Console.WriteLine("Calendar popup is null!");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error showing calendar popup: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        private void OnNotificationBorderTapped(object sender, TappedEventArgs e)
        {
            Console.WriteLine("*** Notification border tapped from XAML handler ***");
            
            try
            {
                // First, close all popups to start fresh
                CloseAllPopups();
                
                // Now, show notifications popup
                if (NotificationsPopupComponent != null)
                {
                    // Show notifications popup
                    NotificationsPopupComponent.IsVisible = true;
                    
                    // Refresh notifications if needed
                    NotificationsPopupComponent.RefreshNotifications();
                    
                    // Show overlay
                    PopupOverlay.IsVisible = true;
                    
                    // Ensure proper z-index
                    NotificationsPopupComponent.ZIndex = 99999;
                    PopupOverlay.ZIndex = 99998;
                    
                    // Force popups to appear above everything else
                    EnsurePopupsAreOnTop();
                    
                    Console.WriteLine("Notifications popup opened successfully");
                }
                else
                {
                    Console.WriteLine("Notifications popup is null!");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error showing notifications popup: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
        
        private void OnPopupOverlayTapped(object sender, TappedEventArgs e)
        {
            CloseAllPopups();
        }
        
        private void CloseAllPopups()
        {
            Console.WriteLine("CloseAllPopups method called");
            
            if (CalendarPopupComponent != null)
            {
                bool wasVisible = CalendarPopupComponent.IsVisible;
                CalendarPopupComponent.IsVisible = false;
                if (wasVisible) Console.WriteLine("Hiding calendar popup");
            }
            
            if (NotificationsPopupComponent != null)
            {
                bool wasVisible = NotificationsPopupComponent.IsVisible;
                NotificationsPopupComponent.IsVisible = false;
                if (wasVisible) Console.WriteLine("Hiding notifications popup");
            }
            
            if (ProfileMenuPopup != null)
            {
                bool wasVisible = ProfileMenuPopup.IsVisible;
                ProfileMenuPopup.IsVisible = false;
                if (wasVisible) Console.WriteLine("Hiding profile menu popup");
            }
            
            if (PopupOverlay != null)
            {
                bool wasVisible = PopupOverlay.IsVisible;
                PopupOverlay.IsVisible = false;
                if (wasVisible) Console.WriteLine("Hiding popup overlay");
            }
        }

        // Method to ensure popups are on top of all other content
        private void EnsurePopupsAreOnTop()
        {
            try
            {
                // Get the main application window
                if (Application.Current?.Windows != null && Application.Current.Windows.Count > 0)
                {
                    var mainWindow = Application.Current.Windows[0];
                    if (mainWindow?.Page != null)
                    {
                        // Get the root layout
                        var rootLayout = GetRootLayout(mainWindow.Page);
                        if (rootLayout != null)
                        {
                            // Add or move popups to the root layout with high Z-index
                            if (PopupOverlay is View popupOverlayView)
                            {
                                RemoveFromCurrentParent(PopupOverlay);
                                AddViewWithHighZIndex(rootLayout, popupOverlayView, 99998);
                            }
                            
                            if (CalendarPopupComponent.IsVisible && CalendarPopupComponent is View calendarPopupView)
                            {
                                RemoveFromCurrentParent(CalendarPopupComponent);
                                AddViewWithHighZIndex(rootLayout, calendarPopupView, 99999);
                            }
                            
                            if (NotificationsPopupComponent.IsVisible && NotificationsPopupComponent is View notificationsPopupView)
                            {
                                RemoveFromCurrentParent(NotificationsPopupComponent);
                                AddViewWithHighZIndex(rootLayout, notificationsPopupView, 99999);
                            }
                            
                            Console.WriteLine("Successfully added popups to root layout");
                        }
                        else
                        {
                            Console.WriteLine("Could not find root layout");
                            
                            // Try alternative approach with Shell or ContentPage
                            if (mainWindow.Page is Shell shell && shell.CurrentItem?.CurrentItem != null)
                            {
                                // For Shell we need to handle differently - use overlay approach
                                UseOverlayApproach();
                            }
                            else if (mainWindow.Page is ContentPage contentPage)
                            {
                                if (contentPage.Content is Layout layout)
                                {
                                    Console.WriteLine("Using ContentPage layout as container");
                                    AddPopupsToLayout(layout);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error ensuring popups are on top: {ex.Message}");
            }
        }

        // Helper method to create an overlay approach if other methods fail
        private void UseOverlayApproach()
        {
            Console.WriteLine("Using overlay approach for popups");
            
            // For this approach, we make sure the popups are properly setup in their own container
            // We'll use their current positioning in the AbsoluteLayout but make sure
            // they have the right Z-index properties set
            
            PopupOverlay.ZIndex = 99998;
            
            if (CalendarPopupComponent.IsVisible)
            {
                CalendarPopupComponent.ZIndex = 99999;
            }
            
            if (NotificationsPopupComponent.IsVisible)
            {
                NotificationsPopupComponent.ZIndex = 99999;
            }
        }

        // Helper to find the root layout container
        private Layout GetRootLayout(Page page)
        {
            try
            {
                // Try to get the navigation root
                if (page is NavigationPage navPage && navPage.CurrentPage != null)
                {
                    page = navPage.CurrentPage;
                }
                else if (page is Shell shell && shell.CurrentPage != null)
                {
                    page = shell.CurrentPage;
                }
                
                // Traverse the visual tree to find a suitable container
                var visualTree = page.GetVisualTreeDescendants();
                foreach (var element in visualTree)
                {
                    if (element is AbsoluteLayout absoluteLayout)
                    {
                        return absoluteLayout;
                    }
                    else if (element is Grid grid && grid.Parent is ContentPage)
                    {
                        return grid;
                    }
                }
                
                // Try to get the Content if it's a Layout
                if (page is ContentPage contentPage && contentPage.Content is Layout layoutContent)
                {
                    return layoutContent;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error finding root layout: {ex.Message}");
            }
            
            return null;
        }

        // Helper to add views with high Z-index
        private void AddViewWithHighZIndex(Layout container, View view, int zIndex)
        {
            try
            {
                if (container is Grid grid)
                {
                    // Check if it's already a child
                    bool alreadyChild = false;
                    foreach (var child in grid.Children)
                    {
                        if (child == view)
                        {
                            alreadyChild = true;
                            break;
                        }
                    }
                    
                    if (!alreadyChild)
                    {
                        grid.Children.Add(view);
                    }
                    
                    // Set the Z-index using attached property
                    view.ZIndex = zIndex;
                }
                else if (container is AbsoluteLayout absoluteLayout)
                {
                    bool alreadyChild = false;
                    foreach (var child in absoluteLayout.Children)
                    {
                        if (child == view)
                        {
                            alreadyChild = true;
                            break;
                        }
                    }
                    
                    if (!alreadyChild)
                    {
                        absoluteLayout.Children.Add(view);
                        AbsoluteLayout.SetLayoutFlags(view, Microsoft.Maui.Layouts.AbsoluteLayoutFlags.All);
                        AbsoluteLayout.SetLayoutBounds(view, new Rect(0, 0, 1, 1));
                    }
                    
                    // Set the Z-index using attached property
                    view.ZIndex = zIndex;
                }
                else if (container is VerticalStackLayout stackLayout)
                {
                    bool alreadyChild = stackLayout.Children.Contains(view);
                    if (!alreadyChild)
                    {
                        stackLayout.Children.Add(view);
                    }
                    
                    // Set the Z-index using attached property
                    view.ZIndex = zIndex;
                }
                else if (container is HorizontalStackLayout hStackLayout)
                {
                    bool alreadyChild = hStackLayout.Children.Contains(view);
                    if (!alreadyChild)
                    {
                        hStackLayout.Children.Add(view);
                    }
                    
                    // Set the Z-index using attached property
                    view.ZIndex = zIndex;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding view with high z-index: {ex.Message}");
            }
        }

        // Helper to add popups to layout container
        private void AddPopupsToLayout(Layout layout)
        {
            if (PopupOverlay is View popupOverlayView)
            {
                RemoveFromCurrentParent(PopupOverlay);
                
                if (layout is Grid grid)
                {
                    grid.Children.Add(popupOverlayView);
                    popupOverlayView.ZIndex = 99998;
                }
                else if (layout is AbsoluteLayout absoluteLayout)
                {
                    absoluteLayout.Children.Add(popupOverlayView);
                    AbsoluteLayout.SetLayoutFlags(popupOverlayView, Microsoft.Maui.Layouts.AbsoluteLayoutFlags.All);
                    AbsoluteLayout.SetLayoutBounds(popupOverlayView, new Rect(0, 0, 1, 1));
                    popupOverlayView.ZIndex = 99998;
                }
                else if (layout is StackLayout stackLayout)
                {
                    stackLayout.Children.Add(popupOverlayView);
                    popupOverlayView.ZIndex = 99998;
                }
            }
            
            if (CalendarPopupComponent.IsVisible && CalendarPopupComponent is View calendarPopupView)
            {
                RemoveFromCurrentParent(CalendarPopupComponent);
                
                if (layout is Grid grid)
                {
                    grid.Children.Add(calendarPopupView);
                    calendarPopupView.ZIndex = 99999;
                }
                else if (layout is AbsoluteLayout absoluteLayout)
                {
                    absoluteLayout.Children.Add(calendarPopupView);
                    // Keep the original layout bounds from XAML
                    AbsoluteLayout.SetLayoutFlags(calendarPopupView, Microsoft.Maui.Layouts.AbsoluteLayoutFlags.PositionProportional);
                    AbsoluteLayout.SetLayoutBounds(calendarPopupView, new Rect(1, 80, 420, 480));
                    calendarPopupView.ZIndex = 99999;
                }
                else if (layout is StackLayout stackLayout)
                {
                    stackLayout.Children.Add(calendarPopupView);
                    calendarPopupView.ZIndex = 99999;
                }
            }
            
            if (NotificationsPopupComponent.IsVisible && NotificationsPopupComponent is View notificationsPopupView)
            {
                RemoveFromCurrentParent(NotificationsPopupComponent);
                
                if (layout is Grid grid)
                {
                    grid.Children.Add(notificationsPopupView);
                    notificationsPopupView.ZIndex = 99999;
                }
                else if (layout is AbsoluteLayout absoluteLayout)
                {
                    absoluteLayout.Children.Add(notificationsPopupView);
                    // Keep the original layout bounds from XAML
                    AbsoluteLayout.SetLayoutFlags(notificationsPopupView, Microsoft.Maui.Layouts.AbsoluteLayoutFlags.PositionProportional);
                    AbsoluteLayout.SetLayoutBounds(notificationsPopupView, new Rect(1, 80, 380, 480));
                    notificationsPopupView.ZIndex = 99999;
                }
                else if (layout is StackLayout stackLayout)
                {
                    stackLayout.Children.Add(notificationsPopupView);
                    notificationsPopupView.ZIndex = 99999;
                }
            }
        }

        // Helper to remove an element from its current parent
        private void RemoveFromCurrentParent(Element element)
        {
            if (element == null || element.Parent == null)
                return;
            
            try
            {
                if (element.Parent is Grid parentGrid && element is IView view)
                {
                    parentGrid.Children.Remove(view);
                }
                else if (element.Parent is ContentView parentView && element is View contentView)
                {
                    if (parentView.Content == contentView)
                    {
                        parentView.Content = null;
                    }
                }
                else
                {
                    Console.WriteLine($"Cannot remove element: Parent is {element.Parent.GetType().Name}, element is {element.GetType().Name}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error removing element from parent: {ex.Message}");
            }
        }
    }
} 