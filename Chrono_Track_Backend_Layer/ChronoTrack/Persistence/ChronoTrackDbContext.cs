using ChronoTrack.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection.Emit;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ChronoTrack.Persistence
{
    public class ChronoTrackDbContext : DbContext
    {
        public ChronoTrackDbContext(DbContextOptions<ChronoTrackDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users => Set<User>();
        public DbSet<UserType> UserTypes => Set<UserType>();
        public DbSet<UserRole> UserRoles => Set<UserRole>();
        public DbSet<Department> Departments => Set<Department>();
        public DbSet<Attendance> Attendances => Set<Attendance>();

        // Special initialization method for existing database
        public async Task EnsureMigrationHistoryExistsAsync()
        {
            try
            {
                // Check if we have any users (to see if the DB is populated)
                if (await Users.AnyAsync())
                {
                    // Check if the migrations history table exists but is empty
                    var conn = Database.GetDbConnection();
                    if (conn.State != System.Data.ConnectionState.Open)
                        await conn.OpenAsync();

                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = @"
                        SELECT EXISTS (
                            SELECT FROM information_schema.tables 
                            WHERE table_schema = 'app'
                            AND table_name = '__EFMigrationsHistory'
                        )";
                    
                    bool migrationTableExists = (bool)await cmd.ExecuteScalarAsync();
                    
                    if (!migrationTableExists)
                    {
                        // Create the migrations history table
                        using var createCmd = conn.CreateCommand();
                        createCmd.CommandText = @"
                            CREATE TABLE IF NOT EXISTS ""__EFMigrationsHistory"" (
                                ""MigrationId"" character varying(150) NOT NULL,
                                ""ProductVersion"" character varying(32) NOT NULL,
                                CONSTRAINT ""PK___EFMigrationsHistory"" PRIMARY KEY (""MigrationId"")
                            );";
                        await createCmd.ExecuteNonQueryAsync();
                        
                        // Insert the migration record as if it was already applied
                        using var insertCmd = conn.CreateCommand();
                        insertCmd.CommandText = @"
                            INSERT INTO ""__EFMigrationsHistory"" (""MigrationId"", ""ProductVersion"")
                            VALUES ('20250323120228_InitialCreate', '9.0.3');";
                        await insertCmd.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error ensuring migration history: {ex.Message}");
            }
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            
            // Suppress the warning about non-deterministic model
            optionsBuilder.ConfigureWarnings(warnings => 
                warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            if (modelBuilder == null)
                throw new ArgumentNullException(nameof(modelBuilder));

            base.OnModelCreating(modelBuilder);

            // Configure schema
            modelBuilder.HasDefaultSchema("app");

            // Configure entities
            modelBuilder.Entity<Models.User>(entity =>
            {
                entity.ToTable("Users");
                entity.Property(e => e.DepartmentID).HasColumnName("DepartmentID");
                entity.Property(e => e.UserRoleID).HasColumnName("UserRoleID");
                entity.Property(e => e.UserTypeID).HasColumnName("UserTypeID");
                
                // Explicitly configure relationships to avoid shadow properties
                entity.HasOne(u => u.Department)
                    .WithMany(d => d.Users)
                    .HasForeignKey(u => u.DepartmentID)
                    .OnDelete(DeleteBehavior.Restrict);
                    
                entity.HasOne(u => u.UserRole)
                    .WithMany(r => r.Users)
                    .HasForeignKey(u => u.UserRoleID)
                    .OnDelete(DeleteBehavior.Restrict);
                    
                entity.HasOne(u => u.UserType)
                    .WithMany()
                    .HasForeignKey(u => u.UserTypeID)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Seed Data
            SeedData(modelBuilder);

            // Set the base entity properties
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                // Configure Created/LastModified properties
                if (typeof(Models.EntityBase).IsAssignableFrom(entityType.ClrType))
                {
                    modelBuilder.Entity(entityType.ClrType)
                        .Property(nameof(Models.EntityBase.Created))
                        .HasDefaultValueSql("CURRENT_TIMESTAMP");
                        
                    modelBuilder.Entity(entityType.ClrType)
                        .Property(nameof(Models.EntityBase.LastModified))
                        .HasDefaultValueSql("CURRENT_TIMESTAMP");
                }
            }
        }

        public override int SaveChanges()
        {
            SetTimestamps();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SetTimestamps();
            return base.SaveChangesAsync(cancellationToken);
        }

        private void SetTimestamps()
        {
            var now = DateTimeOffset.UtcNow;
            
            foreach (var entry in ChangeTracker.Entries<EntityBase>())
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Property(nameof(EntityBase.Created)).CurrentValue = now;
                    entry.Property(nameof(EntityBase.LastModified)).CurrentValue = now;
                }
                else if (entry.State == EntityState.Modified)
                {
                    entry.Property(nameof(EntityBase.LastModified)).CurrentValue = now;
                }
            }
        }

        private static void SeedData(ModelBuilder modelBuilder)
        {
            // Seed UserTypes
            modelBuilder.Entity<UserType>().HasData(
                new UserType { Id = Guid.Parse("c7b013f0-5201-4317-abd8-c211f91b7330"), TypeName = UserTypeEnum.User },
                new UserType { Id = Guid.Parse("8d04dce2-969a-435d-bba4-df3f325983dc"), TypeName = UserTypeEnum.Admin }
            );

            // Seed UserRoles
            modelBuilder.Entity<UserRole>().HasData(
                new UserRole { Id = Guid.Parse("a9f1d24f-5b5a-4a87-a1ed-e6541b4a6fec"), UserRoleName = "Employee" },
                new UserRole { Id = Guid.Parse("e34e5f87-e2c9-4a3a-b234-77d5d7066987"), UserRoleName = "Manager" }
            );

            // Seed Departments
            modelBuilder.Entity<Department>().HasData(
                new Department { Id = Guid.Parse("3d490a70-94ce-4d15-9494-5248280c2ce3"), DepartmentName = "IT" },
                new Department { Id = Guid.Parse("7c9e6679-7425-40de-944b-e07fc1f90ae7"), DepartmentName = "HR" },
                new Department { Id = Guid.Parse("98a52f9d-16be-4a4f-a6c9-6e9df8e1e6eb"), DepartmentName = "Marketing" },
                new Department { Id = Guid.Parse("f8c3de3d-1fea-4d7c-a8b0-29f63c4c3454"), DepartmentName = "Business" },
                new Department { Id = Guid.Parse("6a534922-c788-4386-a38c-aabc856bdca7"), DepartmentName = "Design" },
                new Department { Id = Guid.Parse("f4ed6c3a-c6d3-47b9-b7e5-a67893a8b3a2"), DepartmentName = "Sales" },
                new Department { Id = Guid.Parse("74b2c633-f052-4e50-b00c-9a4f6a2599d6"), DepartmentName = "Management" }
            );

            // Seed Users
            var user1Id = Guid.Parse("98a47581-d326-49e8-bec3-5f6969a2bd9f");
            var user2Id = Guid.Parse("c2e2e7b0-1f2c-4b94-a7c9-4d9b91b9f3a3");

            modelBuilder.Entity<User>().HasData(
                new User
                {
                    Id = user1Id,
                    UserId = "EMP001",
                    FirstName = "John",
                    LastName = "Doe",
                    Gender = GenderEnum.Male,
                    Address = "123 Tech Street, Colombo 01",
                    EmailAddress = "john.doe@timetrack.com",
                    PhoneNumber = "+94 71 1234567",
                    Password = "Pass@123",
                    UserTypeID = Guid.Parse("c7b013f0-5201-4317-abd8-c211f91b7330"),
                    UserRoleID = Guid.Parse("a9f1d24f-5b5a-4a87-a1ed-e6541b4a6fec"),
                    DepartmentID = Guid.Parse("3d490a70-94ce-4d15-9494-5248280c2ce3")
                },
                new User
                {
                    Id = user2Id,
                    UserId = "EMP002",
                    FirstName = "Jane",
                    LastName = "Smith",
                    Gender = GenderEnum.Female,
                    Address = "456 HR Avenue, Colombo 02",
                    EmailAddress = "jane.smith@timetrack.com",
                    PhoneNumber = "+94 71 2345678",
                    Password = "Pass@123",
                    UserTypeID = Guid.Parse("c7b013f0-5201-4317-abd8-c211f91b7330"),
                    UserRoleID = Guid.Parse("e34e5f87-e2c9-4a3a-b234-77d5d7066987"),
                    DepartmentID = Guid.Parse("7c9e6679-7425-40de-944b-e07fc1f90ae7")
                }
            );

            // Seed Attendance Records
            var now = DateTime.UtcNow.Date;
            modelBuilder.Entity<Attendance>().HasData(
                new Attendance
                {
                    Id = Guid.Parse("d8f4b7f0-1b5a-4a85-8f3d-b5c9e6a3d7e1"),
                    UserID = user1Id,
                    AttendanceDate = now,
                    CheckInTime = new TimeSpan(9, 0, 0),
                    CheckOutTime = new TimeSpan(17, 0, 0)
                },
                new Attendance
                {
                    Id = Guid.Parse("e9f5c8f1-2c6b-5b96-9f4e-c6d7f7b4e8f2"),
                    UserID = user2Id,
                    AttendanceDate = now,
                    CheckInTime = new TimeSpan(8, 30, 0),
                    CheckOutTime = new TimeSpan(16, 30, 0)
                }
            );
        }
    }
}
