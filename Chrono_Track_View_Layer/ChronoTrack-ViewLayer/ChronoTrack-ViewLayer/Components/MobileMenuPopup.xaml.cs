using Microsoft.Maui.Controls;
using System;

namespace ChronoTrack_ViewLayer.Components
{
    public partial class MobileMenuPopup : ContentView
    {
        public event EventHandler<string> MenuItemClicked;
        public event EventHandler CloseRequested;

        private const uint AnimationDuration = 250;

        public MobileMenuPopup()
        {
            InitializeComponent();
        }

        public async Task ShowAsync()
        {
            this.IsVisible = true;
            this.Opacity = 0;
            MenuPanel.TranslationX = -280;

            await Task.WhenAll(
                this.FadeTo(1, AnimationDuration),
                MenuPanel.TranslateTo(0, 0, AnimationDuration, Easing.CubicOut)
            );
        }

        public async Task HideAsync()
        {
            await Task.WhenAll(
                this.FadeTo(0, AnimationDuration),
                MenuPanel.TranslateTo(-280, 0, AnimationDuration, Easing.CubicIn)
            );
            this.IsVisible = false;
        }

        private async void OnBackgroundTapped(object sender, EventArgs e)
        {
            await HideAsync();
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }

        private async void OnHomeClicked(object sender, EventArgs e)
        {
            await HideAsync();
            MenuItemClicked?.Invoke(this, "Home");
        }

        private async void OnHistoryClicked(object sender, EventArgs e)
        {
            await HideAsync();
            MenuItemClicked?.Invoke(this, "History");
        }

        private async void OnEmployeesClicked(object sender, EventArgs e)
        {
            await HideAsync();
            MenuItemClicked?.Invoke(this, "Employees");
        }

        private async void OnReportClicked(object sender, EventArgs e)
        {
            await HideAsync();
            MenuItemClicked?.Invoke(this, "Report");
        }

        private async void OnSettingsClicked(object sender, EventArgs e)
        {
            await HideAsync();
            MenuItemClicked?.Invoke(this, "Settings");
        }
    }
} 