using ZamZamApp.Models;
using ZamZamApp.Services;

namespace ZamZamApp.Pages
{
    public partial class EmployeeTasksPage : ContentPage
    {
        private const int DefaultPageSize = 10;
        private int _currentPage = 1;
        private bool _isLoading;

        public EmployeeTasksPage()
        {
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            var user = AuthService.Instance.GetCurrentUser();
            if (user != null)
            {
                UserSubtitleLabel.Text = $"{user.Name} ({user.RoleName})";
            }

            await LoadTasksAsync();
        }

        private async Task LoadTasksAsync()
        {
            if (_isLoading)
                return;

            _isLoading = true;
            LoadingOverlay.IsVisible = true;
            EmptyStateContainer.IsVisible = false;

            try
            {
                string? searchTerm = string.IsNullOrWhiteSpace(SearchEntry.Text) ? null : SearchEntry.Text.Trim();
                var (succeeded, message, data) = await EmployeeTaskService.Instance.GetAllAsync(
                    searchTerm: searchTerm,
                    pageNumber: _currentPage,
                    pageSize: DefaultPageSize);

                if (!succeeded || data == null)
                {
                    BindableLayout.SetItemsSource(TasksTableBody, Array.Empty<EmployeeTaskItem>());
                    EmptyStateLabel.Text = string.IsNullOrWhiteSpace(message)
                        ? "Failed to load employee tasks."
                        : message;
                    EmptyStateContainer.IsVisible = true;
                    UpdatePagination(1, 1, 0, false, false);
                    return;
                }

                BindableLayout.SetItemsSource(TasksTableBody, data.Items);
                EmptyStateLabel.Text = "No employee tasks found.";
                EmptyStateContainer.IsVisible = data.Items.Count == 0;

                UpdatePagination(
                    data.PageNumber,
                    data.TotalPages,
                    data.TotalCount,
                    data.HasPreviousPage,
                    data.HasNextPage);
            }
            catch (Exception ex)
            {
                BindableLayout.SetItemsSource(TasksTableBody, Array.Empty<EmployeeTaskItem>());
                EmptyStateLabel.Text = $"Error: {ex.Message}";
                EmptyStateContainer.IsVisible = true;
            }
            finally
            {
                LoadingOverlay.IsVisible = false;
                _isLoading = false;
            }
        }

        private void UpdatePagination(int pageNumber, int totalPages, int totalCount, bool hasPrev, bool hasNext)
        {
            _currentPage = pageNumber > 0 ? pageNumber : 1;
            int displayTotalPages = totalPages > 0 ? totalPages : 1;

            PaginationInfoLabel.Text = $"Page {_currentPage} of {displayTotalPages} ({totalCount} total)";
            PrevPageButton.IsEnabled = hasPrev;
            NextPageButton.IsEnabled = hasNext;
        }

        private async void OnBackClicked(object? sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("..");
        }

        private async void OnRefreshClicked(object? sender, EventArgs e)
        {
            await LoadTasksAsync();
        }

        private async void OnSearchClicked(object? sender, EventArgs e)
        {
            _currentPage = 1;
            await LoadTasksAsync();
        }

        private async void OnClearSearchClicked(object? sender, EventArgs e)
        {
            SearchEntry.Text = string.Empty;
            _currentPage = 1;
            await LoadTasksAsync();
        }

        private async void OnPrevPageClicked(object? sender, EventArgs e)
        {
            if (_currentPage > 1)
            {
                _currentPage--;
                await LoadTasksAsync();
            }
        }

        private async void OnNextPageClicked(object? sender, EventArgs e)
        {
            _currentPage++;
            await LoadTasksAsync();
        }
    }
}
