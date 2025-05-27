using System;
using System.ComponentModel.DataAnnotations;

namespace ChronoTrack.Models
{
    public class UserRole : EntityBase
    {
        [Required]
        [MaxLength(100)]
        public string UserRoleName { get; set; } = string.Empty;

        public virtual ICollection<User> Users { get; set; } = new List<User>();
    }
}