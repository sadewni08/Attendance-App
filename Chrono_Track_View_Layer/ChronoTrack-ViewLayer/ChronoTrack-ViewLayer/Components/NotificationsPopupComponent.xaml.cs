using System;
using System.Collections.ObjectModel;
using Microsoft.Maui.Controls;

namespace ChronoTrack_ViewLayer.Components
{
    public partial class NotificationsPopupComponent : ContentView
    {
        // Event to notify when the view all notifications button is clicked
        public event EventHandler ViewAllRequested;
        
        // Event to notify when the popup should be closed
        public event EventHandler CloseRequested;
        
        public ObservableCollection<NotificationItem> Notifications { get; private set; }

        public NotificationsPopupComponent()
        {
            InitializeComponent();
            
            // Initialize notification collection
            Notifications = new ObservableCollection<NotificationItem>();
            
            // Load sample notifications
            LoadSampleNotifications();
            
            // Set binding context for data binding
            BindingContext = this;
        }
        
        private void LoadSampleNotifications()
        {
            // Add sample notifications - in a real app, these would come from a service
            Notifications.Add(new NotificationItem
            {
                Title = "New attendance record",
                Message = "Your attendance was recorded on " + DateTime.Now.ToString("MM/dd/yyyy") + " at " + DateTime.Now.ToString("hh:mm tt"),
                TimeSince = "5 minutes ago",
                IsUnread = true
            });
            
            Notifications.Add(new NotificationItem
            {
                Title = "Upcoming meeting",
                Message = "Team standup meeting scheduled for 10:00 AM today",
                TimeSince = "2 hours ago",
                IsUnread = true
            });
            
            Notifications.Add(new NotificationItem
            {
                Title = "Overtime approval",
                Message = "Your overtime request has been approved by the manager",
                TimeSince = "Yesterday at 5:30 PM",
                IsUnread = false
            });
        }
        
        // Call this method to refresh notifications from external source
        public void RefreshNotifications(ObservableCollection<NotificationItem> newNotifications = null)
        {
            if (newNotifications != null)
            {
                Notifications = newNotifications;
            }
            else
            {
                // Reload sample notifications as a fallback
                Notifications.Clear();
                LoadSampleNotifications();
            }
            
            NotificationsListView.ItemsSource = Notifications;
        }
        
        private void OnViewAllNotificationsClicked(object sender, EventArgs e)
        {
            // Notify subscribers that view all was requested
            ViewAllRequested?.Invoke(this, EventArgs.Empty);
            
            // Request to close the popup
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }
        
        private void OnCloseButtonClicked(object sender, EventArgs e)
        {
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }
    }
    
    // Model class for notification items
    public class NotificationItem
    {
        public string Title { get; set; }
        public string Message { get; set; }
        public string TimeSince { get; set; }
        public bool IsUnread { get; set; }
        
        // Background color based on read/unread status
        public string BackgroundColor => IsUnread ? "#F3F9FF" : "White";
    }
} 