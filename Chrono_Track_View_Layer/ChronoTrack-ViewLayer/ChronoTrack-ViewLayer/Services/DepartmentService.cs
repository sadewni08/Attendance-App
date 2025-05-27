using System;
using System.Collections.Generic;

namespace ChronoTrack_ViewLayer.Services
{
    public static class DepartmentService
    {
        // Dictionary mapping department names to their respective GUIDs
        private static readonly Dictionary<string, Guid> DepartmentMapping = new Dictionary<string, Guid>
        {
            // Administrative
            { "IT", Guid.Parse("3d490a70-94ce-4d15-9494-5248280c2ce3") },
            { "HR", Guid.Parse("7c9e6679-7425-40de-944b-e07fc1f90ae7") },
            { "Marketing", Guid.Parse("98a52f9d-16be-4a4f-a6c9-6e9df8e1e6eb") },
            { "Business", Guid.Parse("f8c3de3d-1fea-4d7c-a8b0-29f63c4c3454") },
            { "Design", Guid.Parse("6a534922-c788-4386-a38c-aabc856bdca7") },
            { "Sales", Guid.Parse("f4ed6c3a-c6d3-47b9-b7e5-a67893a8b3a2") },
            { "Management", Guid.Parse("74b2c633-f052-4e50-b00c-9a4f6a2599d6") }
        };

        // Get the GUID for a department name
        public static Guid GetDepartmentId(string departmentName)
        {
            if (string.IsNullOrEmpty(departmentName) || !DepartmentMapping.ContainsKey(departmentName))
            {
                // Return IT department ID as default
                return DepartmentMapping["IT"];
            }
            
            return DepartmentMapping[departmentName];
        }

        // Get the department name for a GUID
        public static string GetDepartmentName(Guid id)
        {
            foreach (var kvp in DepartmentMapping)
            {
                if (kvp.Value == id)
                {
                    return kvp.Key;
                }
            }
            
            // Return IT as default
            return "IT";
        }

        // Get all department names
        public static List<string> GetAllDepartments()
        {
            return new List<string>(DepartmentMapping.Keys);
        }
    }

    public class DepartmentDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }
} 