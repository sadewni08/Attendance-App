using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ChronoTrack_ViewLayer.Models
{
    public class EmployeeSelectionItem : INotifyPropertyChanged
    {
        private bool _isSelected;
        private bool _isCheckedIn;
        private string _status;

        public string UserId { get; set; }
        public string FullName { get; set; }
        public string Department { get; set; }
        public string Role { get; set; }
        public string AttendanceId { get; set; }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsCheckedIn
        {
            get => _isCheckedIn;
            set
            {
                if (_isCheckedIn != value)
                {
                    _isCheckedIn = value;
                    OnPropertyChanged();
                    // Update status text based on checked in state
                    Status = value ? "Checked In" : "Not Checked In";
                }
            }
        }

        public string Status
        {
            get => _status;
            set
            {
                if (_status != value)
                {
                    _status = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsCheckoutEnabled => IsCheckedIn && !string.IsNullOrEmpty(AttendanceId);

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
} 