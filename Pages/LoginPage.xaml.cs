using ZamZamApp.Services;

namespace ZamZamApp.Pages
{
    public partial class LoginPage : ContentPage
    {
        public LoginPage()
        {
            InitializeComponent();
            ApiEndpointLabel.Text = $"🌐 Server: {AppSettings.BaseUrl}";
        }

        private async void OnBackClicked(object? sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("..");
        }

        private void OnTogglePasswordClicked(object? sender, EventArgs e)
        {
            PasswordEntry.IsPassword = !PasswordEntry.IsPassword;
            TogglePasswordButton.Text = PasswordEntry.IsPassword ? "Show" : "Hide";
        }

        private async void OnLoginClicked(object? sender, EventArgs e)
        {
            string email = EmailEntry.Text?.Trim() ?? string.Empty;
            string password = PasswordEntry.Text ?? string.Empty;

            if (string.IsNullOrWhiteSpace(email))
            {
                ShowStatus("Please enter your email address.", isError: true);
                EmailEntry.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                ShowStatus("Please enter your password.", isError: true);
                PasswordEntry.Focus();
                return;
            }

            SetLoading(true);
            ShowStatus("Signing in...", isError: false);

            try
            {
                var (succeeded, message, data) = await AuthService.Instance.LoginAsync(email, password);

                if (!succeeded || data == null)
                {
                    ShowStatus(string.IsNullOrWhiteSpace(message) ? "Login failed." : message, isError: true);
                    SetLoading(false);
                    return;
                }

                string displayName = data.User?.Name ?? email;
                ShowStatus($"Welcome, {displayName}!", isError: false);

                await DisplayAlertAsync(
                    "Login Successful",
                    $"Signed in as {displayName} ({data.User?.RoleName ?? "User"}).",
                    "OK");

                await Shell.Current.GoToAsync($"../{nameof(EmployeeTasksPage)}");
            }
            catch (Exception ex)
            {
                ShowStatus($"Login error: {ex.Message}", isError: true);
            }
            finally
            {
                SetLoading(false);
            }
        }

        private void SetLoading(bool isLoading)
        {
            LoginSubmitButton.IsEnabled = !isLoading;
            LoginSubmitButton.Text = isLoading ? "⏳ Signing In..." : "🔑 Sign In";
            EmailEntry.IsEnabled = !isLoading;
            PasswordEntry.IsEnabled = !isLoading;
        }

        private void ShowStatus(string message, bool isError)
        {
            StatusLabel.Text = message;
            StatusLabel.TextColor = isError ? Colors.Red : Color.FromArgb("#10B981");
            StatusLabel.IsVisible = true;
        }
    }
}
