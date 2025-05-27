namespace ChronoTrack.DTOs
{
    public record CreateUserRoleDto(
        string UserRoleName
    );

    public record UpdateUserRoleDto(
        string UserRoleName
    );

    public record UserRoleDto(
        Guid Id,
        string UserRoleName,
        int AssignedUsers
    );
}
