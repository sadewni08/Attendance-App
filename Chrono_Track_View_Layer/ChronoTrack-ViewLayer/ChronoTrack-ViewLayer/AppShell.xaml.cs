namespace ChronoTrack_ViewLayer;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        RegisterRoutes();
    }

    private void RegisterRoutes()
    {
        Routing.RegisterRoute(nameof(Pages.SummaryPage), typeof(Pages.SummaryPage));
        Routing.RegisterRoute("AttendanceDetailsPage", typeof(Pages.SummaryPage));
    }
} 