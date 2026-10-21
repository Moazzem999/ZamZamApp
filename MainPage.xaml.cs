namespace ZamZamApp
{
    public partial class MainPage : ContentPage
    {
        private const string PrefEmployeeId = "ZamZam_EmployeeId";
        private const string PrefApiTriggerInterval = "ZamZam_ApiTriggerInterval";
        private const string PrefApiUrl = "ZamZam_ApiUrl";

        public MainPage()
        {
            InitializeComponent();
            LoadSavedSettings();
        }

        private void LoadSavedSettings()
        {
            EmployeeIdEntry.Text = Preferences.Default.Get(PrefEmployeeId, string.Empty);
            ApiIntervalEntry.Text = Preferences.Default.Get(PrefApiTriggerInterval, string.Empty);
            ApiUrlEntry.Text = Preferences.Default.Get(PrefApiUrl, string.Empty);
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

            // Validate API Trigger Interval
            if (string.IsNullOrWhiteSpace(intervalText))
            {
                ShowStatus("Please enter an API trigger interval.", isError: true);
                ApiIntervalEntry.Focus();
                return;
            }

            if (!int.TryParse(intervalText, out int interval) || interval <= 0)
            {
                ShowStatus("Trigger interval must be a valid number greater than 0.", isError: true);
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

            // Save settings to persistent storage
            Preferences.Default.Set(PrefEmployeeId, employeeIdText);
            Preferences.Default.Set(PrefApiTriggerInterval, intervalText);
            Preferences.Default.Set(PrefApiUrl, apiUrlText);

            ShowStatus("Settings saved successfully!", isError: false);

            await DisplayAlertAsync("Saved", "Configuration has been saved successfully.", "OK");
        }

        private void ShowStatus(string message, bool isError)
        {
            StatusMessageLabel.Text = message;
            StatusMessageLabel.TextColor = isError ? Colors.Red : Colors.Green;
            StatusMessageLabel.IsVisible = true;
        }
    }
}
