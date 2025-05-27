namespace ChronoTrack_ViewLayer.Models
{
    public class EmployeeDto
    {
        public Guid Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        
        // This property can come from API or be computed from FirstName + LastName
        public string FullName { get; set; } = string.Empty;
        
        public string EmployeeName 
        { 
            get 
            {
                // If FullName is provided by the API, use it
                if (!string.IsNullOrEmpty(FullName))
                    return FullName;
                
                // Otherwise calculate from FirstName and LastName
                return $"{FirstName} {LastName}".Trim();
            }
        }
        
        public string Gender { get; set; } = string.Empty;
        public Guid DepartmentID { get; set; }
        public string Department { get; set; } = string.Empty; // For display purposes
        public Guid UserRoleID { get; set; }
        public string Role { get; set; } = string.Empty;
        public Guid UserTypeID { get; set; }
        public string Address { get; set; } = string.Empty;
        public string EmailAddress { get; set; } = string.Empty; // Match database column name
        public string PhoneNumber { get; set; } = string.Empty;
    }

    public class CreateEmployeeDto
    {
        public string UserId { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string EmailAddress { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Password { get; set; } = "Pass@123";
        public Guid DepartmentID { get; set; }
        public Guid UserRoleID { get; set; }
        public Guid UserTypeID { get; set; } = Guid.Parse("c7b013f0-5201-4317-abd8-c211f91b7330"); // Default to regular User type
        public CreateEmployeeDto()
        {
            // Default constructor
        }
    }

//CHECK USER TYPE ID
    public class UpdateEmployeeDto
    {
        // Explicitly define properties rather than inheriting
        public string UserId { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string EmailAddress { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public Guid DepartmentID { get; set; } // Consistent with database column name
        public Guid UserRoleID { get; set; }  // Consistent with database column name

        public UpdateEmployeeDto()
        {
            // Default constructor
        }
    }

    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new List<T>();
        public int TotalItems { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalItems / (double)PageSize);
        public bool HasNextPage => Page < TotalPages;
        public bool HasPreviousPage => Page > 1;
        public int StartItem => ((Page - 1) * PageSize) + 1;
        public int EndItem => Math.Min(Page * PageSize, TotalItems);
    }
} 