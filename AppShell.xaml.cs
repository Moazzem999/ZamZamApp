using System.Diagnostics;
using ZamZamApp.Services;

namespace ZamZamApp
{
    public partial class AppShell : Shell
    {
        private bool _isHandlingSessionExpired;

        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute(nameof(Pages.LoginPage), typeof(Pages.LoginPage));
            Routing.RegisterRoute(nameof(Pages.EmployeeTasksPage), typeof(Pages.EmployeeTasksPage));

            AuthService.Instance.SessionExpired += OnSessionExpired;
        }

        private async void OnSessionExpired()
        {
            if (_isHandlingSessionExpired)
                return;

            _isHandlingSessionExpired = true;
            try
            {
                if (CurrentPage is Pages.LoginPage)
                    return;

                await DisplayAlertAsync("Session Expired", "Your session has expired. Please sign in again.", "OK");

                if (CurrentPage is Pages.EmployeeTasksPage)
                {
                    await GoToAsync($"../{nameof(Pages.LoginPage)}");
                }
                else if (CurrentPage is not Pages.LoginPage)
                {
                    await GoToAsync(nameof(Pages.LoginPage));
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AppShell] SessionExpired navigation error: {ex.Message}");
            }
            finally
            {
                _isHandlingSessionExpired = false;
            }
        }
    }
}
