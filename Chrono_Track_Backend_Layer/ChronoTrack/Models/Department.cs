using System.ComponentModel.DataAnnotations;
using System;

namespace ChronoTrack.Models
{
    public class Department : EntityBase
    {
        [Required]
        [MaxLength(100)]
        public string DepartmentName { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        public virtual ICollection<User> Users { get; set; } = new List<User>();
    }
}
