using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChronoTrack.Models
{
    public class User : EntityBase
    {
        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [ForeignKey("UserType")]
        public Guid UserTypeID { get; set; }
        public virtual UserType? UserType { get; set; }

        [Required]
        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;

        [Required]
        public GenderEnum Gender { get; set; }

        [Required]
        public string Address { get; set; } = string.Empty;

        [Required]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        public string EmailAddress { get; set; } = string.Empty;

        [Required]
        [ForeignKey("UserRole")]
        public Guid UserRoleID { get; set; }
        public virtual UserRole? UserRole { get; set; }

        [Required]
        [ForeignKey("Department")]
        public Guid DepartmentID { get; set; }
        public virtual Department? Department { get; set; }

        public virtual ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
    }
}

