using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ChronoTrack_ViewLayer.Models
{
    public class AttendanceActivityViewModel : INotifyPropertyChanged
    {
        private string _id;
        public string Id
        {
            get => _id;
            set 
            { 
                if (_id != value)
                {
                    _id = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _employeeName;
        public string EmployeeName
        {
            get => _employeeName;
            set 
            { 
                if (_employeeName != value)
                {
                    _employeeName = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _role;
        public string Role
        {
            get => _role;
            set 
            { 
                if (_role != value)
                {
                    _role = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _department;
        public string Department
        {
            get => _department;
            set 
            { 
                if (_department != value)
                {
                    _department = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _date;
        public string Date
        {
            get => _date;
            set 
            { 
                if (_date != value)
                {
                    _date = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _status;
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

        private string _statusColor;
        public string StatusColor
        {
            get => _statusColor;
            set 
            { 
                if (_statusColor != value)
                {
                    _statusColor = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _checkIn;
        public string CheckIn
        {
            get => _checkIn;
            set 
            { 
                if (_checkIn != value)
                {
                    _checkIn = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _checkOut;
        public string CheckOut
        {
            get => _checkOut;
            set 
            { 
                if (_checkOut != value)
                {
                    _checkOut = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _workingHours;
        public string WorkingHours
        {
            get => _workingHours;
            set 
            { 
                if (_workingHours != value)
                {
                    _workingHours = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
} 