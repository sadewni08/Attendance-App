namespace ChronoTrack.DTOs
{
    // Class-based DTO for better JSON deserialization
    public class CreateUserDto
    {
        public string UserId { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string EmailAddress { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public Guid DepartmentID { get; set; }
        public Guid UserRoleID { get; set; }
        public Guid UserTypeID { get; set; } = Guid.Parse("c7b013f0-5201-4317-abd8-c211f91b7330"); // Default to regular User type
    }

    public record UpdateUserDto(
        string FirstName,
        string LastName,
        string Gender,
        string Address,
        string EmailAddress,
        string PhoneNumber,
        Guid DepartmentID,
        Guid UserRoleID
    );

    public record UserDto(
        Guid Id,
        string UserId,
        string FirstName,
        string LastName,
        string Gender,
        string Department,
        string Role,
        string Address,
        string EmailAddress,
        string PhoneNumber
    );

    public record UserSummaryDto(
        string UserId,
        string FullName,
        string Department,
        string Role,
        string Gender,
        string EmailAddress,
        string PhoneNumber,
        string Address
    );

    public record UserLoginDto(
        string EmailAddress,
        string Password
    );

    public record UserAuthResponseDto(
        Guid Id,
        string UserId,
        string FullName,
        string EmailAddress,
        string Role,
        string Token
    );

    public record MicrosoftAuthDto(
        string Email,
        string Name,
        string IdToken
    );
}
