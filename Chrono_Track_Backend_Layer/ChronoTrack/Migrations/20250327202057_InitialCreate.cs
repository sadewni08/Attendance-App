using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ChronoTrack.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "app");

            migrationBuilder.CreateTable(
                name: "Departments",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DepartmentName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    LastModified = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Departments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserRoles",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserRoleName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    LastModified = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserTypes",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TypeName = table.Column<int>(type: "integer", nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    LastModified = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    UserTypeID = table.Column<Guid>(type: "uuid", nullable: false),
                    FirstName = table.Column<string>(type: "text", nullable: false),
                    LastName = table.Column<string>(type: "text", nullable: false),
                    Password = table.Column<string>(type: "text", nullable: false),
                    Gender = table.Column<int>(type: "integer", nullable: false),
                    Address = table.Column<string>(type: "text", nullable: false),
                    PhoneNumber = table.Column<string>(type: "text", nullable: false),
                    EmailAddress = table.Column<string>(type: "text", nullable: false),
                    UserRoleID = table.Column<Guid>(type: "uuid", nullable: false),
                    DepartmentID = table.Column<Guid>(type: "uuid", nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    LastModified = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_Departments_DepartmentID",
                        column: x => x.DepartmentID,
                        principalSchema: "app",
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Users_UserRoles_UserRoleID",
                        column: x => x.UserRoleID,
                        principalSchema: "app",
                        principalTable: "UserRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Users_UserTypes_UserTypeID",
                        column: x => x.UserTypeID,
                        principalSchema: "app",
                        principalTable: "UserTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Attendances",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserID = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AttendanceDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CheckInTime = table.Column<TimeSpan>(type: "interval", nullable: false),
                    CheckOutTime = table.Column<TimeSpan>(type: "interval", nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    LastModified = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Attendances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Attendances_Users_UserID",
                        column: x => x.UserID,
                        principalSchema: "app",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                schema: "app",
                table: "Departments",
                columns: new[] { "Id", "DepartmentName", "Description" },
                values: new object[,]
                {
                    { new Guid("3d490a70-94ce-4d15-9494-5248280c2ce3"), "IT", "" },
                    { new Guid("6a534922-c788-4386-a38c-aabc856bdca7"), "Design", "" },
                    { new Guid("74b2c633-f052-4e50-b00c-9a4f6a2599d6"), "Management", "" },
                    { new Guid("7c9e6679-7425-40de-944b-e07fc1f90ae7"), "HR", "" },
                    { new Guid("98a52f9d-16be-4a4f-a6c9-6e9df8e1e6eb"), "Marketing", "" },
                    { new Guid("f4ed6c3a-c6d3-47b9-b7e5-a67893a8b3a2"), "Sales", "" },
                    { new Guid("f8c3de3d-1fea-4d7c-a8b0-29f63c4c3454"), "Business", "" }
                });

            migrationBuilder.InsertData(
                schema: "app",
                table: "UserRoles",
                columns: new[] { "Id", "UserRoleName" },
                values: new object[,]
                {
                    { new Guid("a9f1d24f-5b5a-4a87-a1ed-e6541b4a6fec"), "Employee" },
                    { new Guid("e34e5f87-e2c9-4a3a-b234-77d5d7066987"), "Manager" }
                });

            migrationBuilder.InsertData(
                schema: "app",
                table: "UserTypes",
                columns: new[] { "Id", "TypeName" },
                values: new object[,]
                {
                    { new Guid("8d04dce2-969a-435d-bba4-df3f325983dc"), 1 },
                    { new Guid("c7b013f0-5201-4317-abd8-c211f91b7330"), 0 }
                });

            migrationBuilder.InsertData(
                schema: "app",
                table: "Users",
                columns: new[] { "Id", "Address", "DepartmentID", "EmailAddress", "FirstName", "Gender", "LastName", "Password", "PhoneNumber", "UserId", "UserRoleID", "UserTypeID" },
                values: new object[,]
                {
                    { new Guid("98a47581-d326-49e8-bec3-5f6969a2bd9f"), "123 Tech Street, Colombo 01", new Guid("3d490a70-94ce-4d15-9494-5248280c2ce3"), "john.doe@timetrack.com", "John", 0, "Doe", "Pass@123", "+94 71 1234567", "EMP001", new Guid("a9f1d24f-5b5a-4a87-a1ed-e6541b4a6fec"), new Guid("c7b013f0-5201-4317-abd8-c211f91b7330") },
                    { new Guid("c2e2e7b0-1f2c-4b94-a7c9-4d9b91b9f3a3"), "456 HR Avenue, Colombo 02", new Guid("7c9e6679-7425-40de-944b-e07fc1f90ae7"), "jane.smith@timetrack.com", "Jane", 1, "Smith", "Pass@123", "+94 71 2345678", "EMP002", new Guid("e34e5f87-e2c9-4a3a-b234-77d5d7066987"), new Guid("c7b013f0-5201-4317-abd8-c211f91b7330") }
                });

            migrationBuilder.InsertData(
                schema: "app",
                table: "Attendances",
                columns: new[] { "Id", "AttendanceDate", "CheckInTime", "CheckOutTime", "UserID", "UserId" },
                values: new object[,]
                {
                    { new Guid("d8f4b7f0-1b5a-4a85-8f3d-b5c9e6a3d7e1"), new DateTime(2025, 3, 27, 0, 0, 0, 0, DateTimeKind.Utc), new TimeSpan(0, 9, 0, 0, 0), new TimeSpan(0, 17, 0, 0, 0), new Guid("98a47581-d326-49e8-bec3-5f6969a2bd9f"), new Guid("00000000-0000-0000-0000-000000000000") },
                    { new Guid("e9f5c8f1-2c6b-5b96-9f4e-c6d7f7b4e8f2"), new DateTime(2025, 3, 27, 0, 0, 0, 0, DateTimeKind.Utc), new TimeSpan(0, 8, 30, 0, 0), new TimeSpan(0, 16, 30, 0, 0), new Guid("c2e2e7b0-1f2c-4b94-a7c9-4d9b91b9f3a3"), new Guid("00000000-0000-0000-0000-000000000000") }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_UserID",
                schema: "app",
                table: "Attendances",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_Users_DepartmentID",
                schema: "app",
                table: "Users",
                column: "DepartmentID");

            migrationBuilder.CreateIndex(
                name: "IX_Users_UserRoleID",
                schema: "app",
                table: "Users",
                column: "UserRoleID");

            migrationBuilder.CreateIndex(
                name: "IX_Users_UserTypeID",
                schema: "app",
                table: "Users",
                column: "UserTypeID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Attendances",
                schema: "app");

            migrationBuilder.DropTable(
                name: "Users",
                schema: "app");

            migrationBuilder.DropTable(
                name: "Departments",
                schema: "app");

            migrationBuilder.DropTable(
                name: "UserRoles",
                schema: "app");

            migrationBuilder.DropTable(
                name: "UserTypes",
                schema: "app");
        }
    }
}
