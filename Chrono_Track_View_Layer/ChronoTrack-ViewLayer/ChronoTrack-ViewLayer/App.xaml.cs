namespace ChronoTrack_ViewLayer
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            MainPage = new NavigationPage(new MainPage());
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var window = new Window(MainPage) { Title = "ChronoTrack-ViewLayer" };
            return window;
        }
    }
}
