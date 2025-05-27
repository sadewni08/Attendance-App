using System;
using ChronoTrack_ViewLayer.Services.Interfaces;

namespace ChronoTrack_ViewLayer.Services
{
    public static class ServiceProviderExtensions
    {
        /// <summary>
        /// Gets a service of the specified type from the service provider
        /// </summary>
        public static T GetService<T>() where T : class
        {
            Type type = typeof(T);
            
            if (type == typeof(IAttendanceService))
                return (T)(object)new AttendanceService();
                
            if (type == typeof(IEmployeeService))
                return (T)(object)new EmployeeService();
                
            throw new ArgumentException($"Service of type {type.Name} is not registered");
        }
    }
} 