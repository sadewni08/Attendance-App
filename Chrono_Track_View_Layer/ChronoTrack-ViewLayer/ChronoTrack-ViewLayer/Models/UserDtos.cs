namespace ChronoTrack_ViewLayer.Models
{
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
} 