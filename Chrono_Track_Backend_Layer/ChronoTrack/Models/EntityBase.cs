using System;
using System.ComponentModel.DataAnnotations;

namespace ChronoTrack.Models
{
    public abstract class EntityBase
    {
        public Guid Id { get; init; }

        public DateTimeOffset Created { get; private set; }
        public DateTimeOffset LastModified { get; private set; }

        public void UpdateLastModified()
        {
            LastModified = DateTimeOffset.UtcNow;
        }
    }
}
