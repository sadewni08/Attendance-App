namespace ChronoTrack.DTOs
{
    public record ExportRequestDto(
        DateTime? FromDate,
        DateTime? ToDate,
        Guid? DepartmentId,
        Guid? UserId,
        string ExportFormat = "xlsx" // xlsx, csv, pdf
    );

    public record AttendanceReportDto(
        DateTime FromDate,
        DateTime ToDate,
        int TotalEmployees,
        int TotalWorkingDays,
        double AverageAttendanceRate,
        List<DailyAttendanceSummaryDto> DailySummaries
    );

    public record DailyAttendanceSummaryDto(
        DateTime Date,
        int PresentEmployees,
        int AbsentEmployees,
        double AttendanceRate,
        List<EmployeeAttendanceDto> EmployeeAttendances
    );

    public record EmployeeAttendanceDto(
        Guid UserId,
        string FullName,
        string Department,
        TimeSpan CheckInTime,
        TimeSpan? CheckOutTime,
        TimeSpan? WorkDuration,
        string Status // Present, Absent, Late, etc.
    );
}
