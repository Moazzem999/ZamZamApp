using ZamZamApp.Services;

namespace ZamZamApp
{
    public partial class MainPage : ContentPage
    {
        private const string PrefEmployeeId = TrackingManager.PrefEmployeeId;
        private const string PrefApiTriggerInterval = TrackingManager.PrefApiTriggerInterval;
        private const string PrefApiUrl = TrackingManager.PrefApiUrl;
        private const string PrefIsSaved = TrackingManager.PrefIsSaved;

        public MainPage()
        {
            InitializeComponent();
#if DEBUG
            DebugResetButton.IsVisible = true;
#endif
            LoadSavedSettings();

            TrackingManager.Instance.TrackingLogEvent += OnTrackingLogReceived;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // Request location and battery permissions on launch
            await TrackingManager.Instance.RequestPermissionsAndBatteryOptimizationAsync();

            // If already configured and saved, ensure background tracking is actively running
            if (Preferences.Default.Get(PrefIsSaved, false))
            {
                TrackingManager.Instance.StartTracking();
            }
        }

        private void OnTrackingLogReceived(string message, bool isSuccess)
        {
            if (Preferences.Default.Get(PrefIsSaved, false))
            {
                ShowStatus(message, isError: !isSuccess);
            }
        }

        private void LoadSavedSettings()
        {
            EmployeeIdEntry.Text = Preferences.Default.Get(PrefEmployeeId, string.Empty);
            ApiIntervalEntry.Text = Preferences.Default.Get(PrefApiTriggerInterval, string.Empty);
            ApiUrlEntry.Text = Preferences.Default.Get(PrefApiUrl, string.Empty);

            if (Preferences.Default.Get(PrefIsSaved, false))
            {
                DisableFormForLifetime();
            }
        }

        private void DisableFormForLifetime()
        {
            SaveSettingsButton.IsEnabled = false;
            SaveSettingsButton.Text = "Settings Saved & Locked";

            EmployeeIdEntry.IsReadOnly = true;
            ApiIntervalEntry.IsReadOnly = true;
            ApiUrlEntry.IsReadOnly = true;

            ShowStatus("Tracking active in background. Reinstall app to change settings.", isError: false);
        }

        private void OnNumericEntryTextChanged(object? sender, TextChangedEventArgs e)
        {
            if (sender is not Entry entry || string.IsNullOrEmpty(e.NewTextValue))
                return;

            // Enforce digits only
            var digitsOnly = new string(e.NewTextValue.Where(char.IsDigit).ToArray());
            if (digitsOnly != e.NewTextValue)
            {
                entry.Text = digitsOnly;
            }
        }

        private async void OnSaveClicked(object? sender, EventArgs e)
        {
            // Safeguard if already saved
            if (Preferences.Default.Get(PrefIsSaved, false))
            {
                DisableFormForLifetime();
                return;
            }

            string employeeIdText = EmployeeIdEntry.Text?.Trim() ?? string.Empty;
            string intervalText = ApiIntervalEntry.Text?.Trim() ?? string.Empty;
            string apiUrlText = ApiUrlEntry.Text?.Trim() ?? string.Empty;

            // Validate Employee ID
            if (string.IsNullOrWhiteSpace(employeeIdText))
            {
                ShowStatus("Please enter an Employee ID.", isError: true);
                EmployeeIdEntry.Focus();
                return;
            }

            if (!long.TryParse(employeeIdText, out long employeeId) || employeeId <= 0)
            {
                ShowStatus("Employee ID must be a valid positive number.", isError: true);
                EmployeeIdEntry.Focus();
                return;
            }

            // Validate API Trigger Interval (minutes)
            if (string.IsNullOrWhiteSpace(intervalText))
            {
                ShowStatus("Please enter an API trigger interval (in minutes).", isError: true);
                ApiIntervalEntry.Focus();
                return;
            }

            if (!int.TryParse(intervalText, out int interval) || interval <= 0)
            {
                ShowStatus("Trigger interval must be a valid number of minutes greater than 0.", isError: true);
                ApiIntervalEntry.Focus();
                return;
            }

            // Validate API URL
            if (string.IsNullOrWhiteSpace(apiUrlText))
            {
                ShowStatus("Please enter an API URL.", isError: true);
                ApiUrlEntry.Focus();
                return;
            }

            if (!Uri.TryCreate(apiUrlText, UriKind.Absolute, out Uri? uriResult) ||
                (uriResult.Scheme != Uri.UriSchemeHttp && uriResult.Scheme != Uri.UriSchemeHttps))
            {
                ShowStatus("Please enter a valid URL (starting with http:// or https://).", isError: true);
                ApiUrlEntry.Focus();
                return;
            }

            // Ensure permissions and battery optimization exemption are requested
            await TrackingManager.Instance.RequestPermissionsAndBatteryOptimizationAsync();

            // Save settings to persistent storage
            Preferences.Default.Set(PrefEmployeeId, employeeIdText);
            Preferences.Default.Set(PrefApiTriggerInterval, intervalText);
            Preferences.Default.Set(PrefApiUrl, apiUrlText);
            Preferences.Default.Set(PrefIsSaved, true);

            // Permanently lock the form and button
            DisableFormForLifetime();

            // Start the background tracking service immediately
            TrackingManager.Instance.StartTracking();

            await DisplayAlertAsync("Saved & Tracking Started", "Settings are locked. Background tracking has started and will continue running even if the app is closed.", "OK");
        }

#if DEBUG
        private async void OnDebugResetClicked(object? sender, EventArgs e)
        {
            bool confirm = await DisplayAlertAsync("Reset Form (Debug)", "Stop tracking, clear all saved settings, and unlock the form for testing?", "Yes, Reset", "Cancel");
            if (!confirm)
                return;

            // Stop background tracking
            TrackingManager.Instance.StopTracking();

            Preferences.Default.Remove(PrefEmployeeId);
            Preferences.Default.Remove(PrefApiTriggerInterval);
            Preferences.Default.Remove(PrefApiUrl);
            Preferences.Default.Remove(PrefIsSaved);

            EmployeeIdEntry.Text = string.Empty;
            ApiIntervalEntry.Text = string.Empty;
            ApiUrlEntry.Text = string.Empty;

            EmployeeIdEntry.IsReadOnly = false;
            ApiIntervalEntry.IsReadOnly = false;
            ApiUrlEntry.IsReadOnly = false;

            SaveSettingsButton.IsEnabled = true;
            SaveSettingsButton.Text = "Save Settings";

            StatusMessageLabel.Text = string.Empty;
            StatusMessageLabel.IsVisible = false;

            await DisplayAlertAsync("Reset Complete", "Tracking stopped and settings reset. You can now enter and test new credentials.", "OK");
        }
#else
        private void OnDebugResetClicked(object? sender, EventArgs e)
        {
        }
#endif

        private void ShowStatus(string message, bool isError)
        {
            StatusMessageLabel.Text = message;
            StatusMessageLabel.TextColor = isError ? Colors.Red : Colors.Green;
            StatusMessageLabel.IsVisible = true;
        }
    }
}
