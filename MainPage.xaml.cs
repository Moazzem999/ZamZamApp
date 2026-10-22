using ZamZamApp.Services;

namespace ZamZamApp
{
    public partial class MainPage : ContentPage
    {
        private const string PrefEmployeeId = TrackingManager.PrefEmployeeId;
        private const string PrefDateOfBirth = TrackingManager.PrefDateOfBirth;
        private const string PrefMobile = TrackingManager.PrefMobile;
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
            UpdateDashboardUI();

            TrackingManager.Instance.TrackingLogEvent += OnTrackingLogReceived;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            UpdateDashboardUI();

            // Request location and battery permissions on launch
            await TrackingManager.Instance.RequestPermissionsAndBatteryOptimizationAsync();

            // If already configured and saved, ensure background tracking is actively running
            if (Preferences.Default.Get(PrefIsSaved, false))
            {
                TrackingManager.Instance.StartTracking();
            }

            UpdateLocationPreview();
        }

        private void UpdateDashboardUI()
        {
            bool isSaved = Preferences.Default.Get(PrefIsSaved, false);

            // Update Battery using native BatteryManager driver (eliminating N/A on modern Android devices)
            int batteryLevel = TrackingManager.GetBatteryPercentage();
            BatteryValueLabel.Text = $"{batteryLevel}%";

            // Update Interval
            string interval = Preferences.Default.Get(PrefApiTriggerInterval, string.Empty);
            IntervalValueLabel.Text = !string.IsNullOrEmpty(interval) ? $"{interval}m" : "--";

            // Update Employee ID
            string empId = Preferences.Default.Get(PrefEmployeeId, string.Empty);
            EmployeeIdValueLabel.Text = !string.IsNullOrEmpty(empId) ? $"#{empId}" : "None";

            // Update Status Badge
            if (Application.Current != null)
            {
                if (isSaved)
                {
                    var green = Color.FromArgb("#10B981");
                    var greenBg = Color.FromArgb("#ECFDF5");

                    StatusBadgeBorder.Stroke = green;
                    StatusBadgeDot.Fill = green;
                    StatusBadgeText.Text = "Active • Tracking";
                    StatusBadgeText.TextColor = green;
                    StatusBadgeBorder.BackgroundColor = Application.Current.RequestedTheme == AppTheme.Dark
                        ? Color.FromArgb("#064E3B")
                        : greenBg;
                }
                else
                {
                    var amber = Color.FromArgb("#F59E0B");
                    var amberBg = Color.FromArgb("#FFFBEB");

                    StatusBadgeBorder.Stroke = amber;
                    StatusBadgeDot.Fill = amber;
                    StatusBadgeText.Text = "Setup Required";
                    StatusBadgeText.TextColor = amber;
                    StatusBadgeBorder.BackgroundColor = Application.Current.RequestedTheme == AppTheme.Dark
                        ? Color.FromArgb("#78350F")
                        : amberBg;
                }
            }
        }

        private void UpdateLocationPreview()
        {
            Task.Run(async () =>
            {
                try
                {
                    var loc = await Geolocation.Default.GetLastKnownLocationAsync();
                    if (loc != null)
                    {
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            CoordinatesLabel.Text = $"🌐 Lat: {loc.Latitude:F4}, Lon: {loc.Longitude:F4} (±{loc.Accuracy:F0}m)";
                        });
                    }
                }
                catch
                {
                }
            });
        }

        private void OnTrackingLogReceived(string message, bool isSuccess)
        {
            ConsoleLogLabel.Text = $"[{DateTime.Now:HH:mm:ss}] {message}";
            LastSyncLabel.Text = $"🕒 Last Trigger: {DateTime.Now:hh:mm:ss tt}";

            if (Preferences.Default.Get(PrefIsSaved, false))
            {
                ShowStatus(message, isError: !isSuccess);
            }

            UpdateLocationPreview();
        }

        private void LoadSavedSettings()
        {
            EmployeeIdEntry.Text = Preferences.Default.Get(PrefEmployeeId, string.Empty);
            DateOfBirthEntry.Text = Preferences.Default.Get(PrefDateOfBirth, string.Empty);
            MobileEntry.Text = Preferences.Default.Get(PrefMobile, string.Empty);
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
            SaveSettingsButton.Text = "🔒 Settings Locked & Active";
            SaveSettingsButton.Opacity = 0.7;

            EmployeeIdEntry.IsReadOnly = true;
            DateOfBirthEntry.IsReadOnly = true;
            MobileEntry.IsReadOnly = true;
            ApiIntervalEntry.IsReadOnly = true;
            ApiUrlEntry.IsReadOnly = true;

            UpdateDashboardUI();
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
            string dobText = DateOfBirthEntry.Text?.Trim() ?? string.Empty;
            string mobileText = MobileEntry.Text?.Trim() ?? string.Empty;
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

            // Validate Date of Birth
            if (string.IsNullOrWhiteSpace(dobText))
            {
                ShowStatus("Please enter Date of Birth (YYYY-MM-DD).", isError: true);
                DateOfBirthEntry.Focus();
                return;
            }

            if (!DateTime.TryParse(dobText, out DateTime dobDate) || dobDate > DateTime.Today || dobDate < DateTime.Today.AddYears(-120))
            {
                ShowStatus("Please enter a valid Date of Birth (YYYY-MM-DD) not in the future.", isError: true);
                DateOfBirthEntry.Focus();
                return;
            }

            string formattedDob = dobDate.ToString("yyyy-MM-dd");

            // Validate Mobile Number
            if (string.IsNullOrWhiteSpace(mobileText))
            {
                ShowStatus("Please enter a Mobile number.", isError: true);
                MobileEntry.Focus();
                return;
            }

            if (mobileText.Length < 11)
            {
                ShowStatus("Please enter a valid Mobile number.", isError: true);
                MobileEntry.Focus();
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

            // Set UI to verifying state
            SaveSettingsButton.IsEnabled = false;
            SaveSettingsButton.Text = "⏳ Verifying with API...";
            ShowStatus("Calling API to verify credentials...", isError: false);

            try
            {
                // Call API before saving and locking
                var testPayload = await TrackingManager.Instance.BuildCurrentPayloadAsync(employeeId, formattedDob, mobileText);
                var (succeeded, responseText) = await TrackingManager.Instance.SendPayloadAsync(testPayload, apiUrlText);

                if (!succeeded)
                {
                    // If API returns succeeded = false or fails, show response and DO NOT lock
                    string displayResponse = !string.IsNullOrWhiteSpace(responseText) 
                        ? responseText 
                        : "API returned succeeded = false or failed to respond.";

                    ShowStatus($"API Verification Failed: {displayResponse}", isError: true);

                    SaveSettingsButton.IsEnabled = true;
                    SaveSettingsButton.Text = "🔒 Save & Activate Tracker";

                    await DisplayAlertAsync("API Verification Failed", displayResponse, "OK");
                    return;
                }

                // If API returns succeeded = true, save settings and permanently lock
                Preferences.Default.Set(PrefEmployeeId, employeeIdText);
                Preferences.Default.Set(PrefDateOfBirth, formattedDob);
                Preferences.Default.Set(PrefMobile, mobileText);
                Preferences.Default.Set(PrefApiTriggerInterval, intervalText);
                Preferences.Default.Set(PrefApiUrl, apiUrlText);
                Preferences.Default.Set(PrefIsSaved, true);

                // Permanently lock the form and button
                DisableFormForLifetime();

                // Start the background tracking service immediately
                TrackingManager.Instance.StartTracking();

                await DisplayAlertAsync("Verified & Tracking Started", "API verified successfully (succeeded = true). Settings are locked and background tracking has started.", "OK");
            }
            catch (Exception ex)
            {
                ShowStatus($"Error contacting API: {ex.Message}", isError: true);
                SaveSettingsButton.IsEnabled = true;
                SaveSettingsButton.Text = "🔒 Save & Activate Tracker";
                await DisplayAlertAsync("Connection Error", $"Failed to contact server: {ex.Message}", "OK");
            }
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
            Preferences.Default.Remove(PrefDateOfBirth);
            Preferences.Default.Remove(PrefMobile);
            Preferences.Default.Remove(PrefApiTriggerInterval);
            Preferences.Default.Remove(PrefApiUrl);
            Preferences.Default.Remove(PrefIsSaved);

            EmployeeIdEntry.Text = string.Empty;
            DateOfBirthEntry.Text = string.Empty;
            MobileEntry.Text = string.Empty;
            ApiIntervalEntry.Text = string.Empty;
            ApiUrlEntry.Text = string.Empty;

            EmployeeIdEntry.IsReadOnly = false;
            DateOfBirthEntry.IsReadOnly = false;
            MobileEntry.IsReadOnly = false;
            ApiIntervalEntry.IsReadOnly = false;
            ApiUrlEntry.IsReadOnly = false;

            SaveSettingsButton.IsEnabled = true;
            SaveSettingsButton.Text = "🔒 Save & Activate Tracker";
            SaveSettingsButton.Opacity = 1.0;

            StatusMessageLabel.Text = string.Empty;
            StatusMessageLabel.IsVisible = false;

            ConsoleLogLabel.Text = "Settings reset. Ready for configuration.";
            UpdateDashboardUI();

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
