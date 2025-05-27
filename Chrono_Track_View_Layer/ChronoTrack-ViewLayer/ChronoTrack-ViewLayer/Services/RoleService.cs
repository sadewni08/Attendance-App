using System;
using System.Collections.Generic;
using System.Linq;

namespace ChronoTrack_ViewLayer.Services
{
    public static class RoleService
    {
        // Dictionary to map role names to IDs - matches backend UserRoleConfiguration
        private static readonly Dictionary<string, Guid> RoleMapping = new()
        {
            // Administrative roles
            { "Administrator", new Guid("2c5e174e-3b0e-446f-86af-483d56fd7210") },
            { "Manager", new Guid("e34e5f87-e2c9-4a3a-b234-77d5d7066987") },
            { "Employee", new Guid("a9f1d24f-5b5a-4a87-a1ed-e6541b4a6fec") },
            
            // IT Department roles
            { "Software Developer", new Guid("11111111-1111-1111-2222-111111111111") },
            { "DevOps Engineer", new Guid("66666666-6666-6666-2222-666666666666") },
            { "System Analyst", new Guid("88888888-8888-8888-2222-888888888888") },
            { "QA Engineer", new Guid("55555555-5555-5555-2222-555555555555") },
            
            // Design Department roles
            { "UI/UX Designer", new Guid("22222222-2222-2222-2222-222222222222") },
            
            // Management Department roles
            { "Project Manager", new Guid("33333333-3333-3333-2222-333333333333") },
            { "Product Manager", new Guid("77777777-7777-7777-2222-777777777777") },
            
            // Business Department roles
            { "Business Analyst", new Guid("44444444-4444-4444-2222-444444444444") },
            
            // HR Department roles
            { "HR Specialist", new Guid("99999999-9999-9999-2222-999999999999") },
            
            // Marketing Department roles
            { "Marketing Specialist", new Guid("aaaaaaaa-aaaa-aaaa-2222-aaaaaaaaaaaa") },
            
            // Sales Department roles
            { "Sales Representative", new Guid("bbbbbbbb-bbbb-bbbb-2222-bbbbbbbbbbbb") }
        };

        // Dictionary to map department IDs to roles
        private static readonly Dictionary<string, List<string>> DepartmentRoles = new()
        {
            { "IT", new List<string> { "Software Developer", "DevOps Engineer", "System Analyst", "QA Engineer" } },
            { "Design", new List<string> { "UI/UX Designer" } },
            { "Management", new List<string> { "Project Manager", "Product Manager" } },
            { "Business", new List<string> { "Business Analyst" } },
            { "HR", new List<string> { "HR Specialist" } },
            { "Marketing", new List<string> { "Marketing Specialist" } },
            { "Sales", new List<string> { "Sales Representative" } }
        };

        // Get role ID from name
        public static Guid GetRoleId(string roleName)
        {
            if (RoleMapping.TryGetValue(roleName, out Guid id))
            {
                return id;
            }

            // Return default ID if role name not found
            return new Guid("a9f1d24f-5b5a-4a87-a1ed-e6541b4a6fec"); // Default to Employee
        }

        // Get role name from ID
        public static string GetRoleName(Guid roleId)
        {
            foreach (var kvp in RoleMapping)
            {
                if (kvp.Value == roleId)
                {
                    return kvp.Key;
                }
            }

            return "Employee"; // Default role
        }

        // Get all roles for a specific department
        public static List<string> GetRolesForDepartment(string departmentName)
        {
            if (DepartmentRoles.TryGetValue(departmentName, out List<string> roles))
            {
                return roles;
            }

            return new List<string> { "Employee" }; // Default role if department not found
        }

        // Get all available roles
        public static List<string> GetAllRoles()
        {
            return RoleMapping.Keys.ToList();
        }

        // Get all department-specific roles (excluding administrative roles)
        public static List<string> GetAllDepartmentRoles()
        {
            return RoleMapping.Keys
                .Where(r => r != "Administrator" && r != "Manager" && r != "Employee")
                .ToList();
        }
    }
} 