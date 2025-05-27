using System;
using System.ComponentModel.DataAnnotations;

namespace ChronoTrack.Models
{
    public class UserType : EntityBase
    {
        [Required]
        public UserTypeEnum TypeName { get; set; }
    }

    public enum UserTypeEnum
    {
        User,
        Admin
    }
}