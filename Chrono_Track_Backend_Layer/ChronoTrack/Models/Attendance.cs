using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChronoTrack.Models
{
    public class Attendance : EntityBase
    {
        [ForeignKey("User")]
        public Guid UserID { get; set; }
        public Guid UserId { get; set; }
        public virtual User? User { get; set; }

        [Required]
        public DateTime AttendanceDate { get; set; }

        [Required]
        public TimeSpan CheckInTime { get; set; }

        [Required]
        public TimeSpan CheckOutTime { get; set; }
    }
}
