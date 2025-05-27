namespace ChronoTrack.DTOs
{
    public record CreateDepartmentDto(
        string DepartmentName,
        string Description
    );

    public record UpdateDepartmentDto(
        string DepartmentName,
        string Description
    );

    public record DepartmentDto(
        Guid Id,
        string DepartmentName,
        string Description,
        int EmployeeCount
    );

    public record DepartmentSummaryDto(
        Guid Id,
        string DepartmentName,
        int EmployeeCount,
        double AverageAttendance,
        double AverageWorkingHours
    );
}
