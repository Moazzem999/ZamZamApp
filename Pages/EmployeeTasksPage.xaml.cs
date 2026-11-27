using ZamZamApp.Models;
using ZamZamApp.Services;

namespace ZamZamApp.Pages
{
    public partial class EmployeeTasksPage : ContentPage
    {
        private int _pageSize = 10;
        private int _currentPage = 1;
        private bool _isLoading;
        private bool _isCreating;
        private bool _isInitializing = true;

        public EmployeeTasksPage()
        {
            InitializeComponent();
            PageSizePicker.SelectedIndex = 0;
            _isInitializing = false;
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
                    pageSize: _pageSize);

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

        private async void OnPageSizeChanged(object? sender, EventArgs e)
        {
            if (_isInitializing || PageSizePicker.SelectedItem is not int selectedSize)
                return;

            if (selectedSize == _pageSize)
                return;

            _pageSize = selectedSize;
            _currentPage = 1;
            await LoadTasksAsync();
        }

        private async void OnPreviewTaskClicked(object? sender, EventArgs e)
        {
            if (sender is not Button button || button.CommandParameter is not EmployeeTaskItem task)
                return;

            string details =
                $"Task ID: #{task.Id}\n" +
                $"Title: {task.Title}\n" +
                $"Employee: {task.EmployeeName} (ID: {task.EmployeeId})\n" +
                $"Scheduled: {task.ScheduledOnDisplay}\n" +
                $"Status: {task.StatusDisplay}\n" +
                $"Description:\n{(string.IsNullOrWhiteSpace(task.Description) ? "-" : task.Description)}";

            await DisplayAlertAsync("Task Preview", details, "Close");
        }

        private async void OnDeleteTaskClicked(object? sender, EventArgs e)
        {
            if (_isLoading || sender is not Button button || button.CommandParameter is not EmployeeTaskItem task)
                return;

            bool confirmed = await DisplayAlertAsync(
                "Delete Task",
                $"Are you sure you want to delete \"{task.Title}\" (#{task.Id})?",
                "Delete",
                "Cancel");

            if (!confirmed)
                return;

            _isLoading = true;
            LoadingOverlay.IsVisible = true;
            bool succeeded = false;
            string message = string.Empty;

            try
            {
                (succeeded, message) = await EmployeeTaskService.Instance.DeleteAsync(task.Id);
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }
            finally
            {
                LoadingOverlay.IsVisible = false;
                _isLoading = false;
            }

            if (!succeeded)
            {
                if (!AuthService.Instance.IsLoggedIn)
                    return;

                await DisplayAlertAsync(
                    "Delete Failed",
                    string.IsNullOrWhiteSpace(message) ? "Failed to delete employee task." : message,
                    "OK");
                return;
            }

            await DisplayAlertAsync(
                "Deleted",
                string.IsNullOrWhiteSpace(message) ? "Employee task successfully deleted." : message,
                "OK");
            await LoadTasksAsync();
        }

        private void OnCreateTaskClicked(object? sender, EventArgs e)
        {
            CreateTitleEntry.Text = string.Empty;
            CreateDescriptionEditor.Text = string.Empty;
            CreateScheduledDatePicker.Date = DateTime.Today;
            CreateScheduledTimePicker.Time = DateTime.Now.TimeOfDay;
            CreateFormErrorLabel.Text = string.Empty;
            CreateFormErrorLabel.IsVisible = false;
            CreateTaskOverlay.IsVisible = true;
            CreateTitleEntry.Focus();
        }

        private void OnCancelCreateTaskClicked(object? sender, EventArgs e)
        {
            if (_isCreating)
                return;

            CreateTaskOverlay.IsVisible = false;
        }

        private async void OnSubmitCreateTaskClicked(object? sender, EventArgs e)
        {
            if (_isCreating)
                return;

            string title = CreateTitleEntry.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(title))
            {
                CreateFormErrorLabel.Text = "Please enter a task title.";
                CreateFormErrorLabel.IsVisible = true;
                CreateTitleEntry.Focus();
                return;
            }

            CreateFormErrorLabel.IsVisible = false;
            DateTime scheduledDate = CreateScheduledDatePicker.Date ?? DateTime.Today;
            TimeSpan scheduledTime = CreateScheduledTimePicker.Time ?? TimeSpan.Zero;
            DateTime localScheduledOn = scheduledDate.Date.Add(scheduledTime);

            var payload = new EmployeeTaskCreateRequest
            {
                Id = 0,
                Title = title,
                Description = CreateDescriptionEditor.Text?.Trim() ?? string.Empty,
                ScheduledOn = DateTime.SpecifyKind(localScheduledOn, DateTimeKind.Local).ToUniversalTime()
            };

            SetCreateLoading(true);
            bool succeeded = false;
            string message = string.Empty;

            try
            {
                (succeeded, message, _) = await EmployeeTaskService.Instance.CreateAsync(payload);
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }
            finally
            {
                SetCreateLoading(false);
            }

            if (!succeeded)
            {
                if (!AuthService.Instance.IsLoggedIn)
                {
                    CreateTaskOverlay.IsVisible = false;
                    return;
                }

                CreateFormErrorLabel.Text = string.IsNullOrWhiteSpace(message)
                    ? "Failed to create employee task."
                    : message;
                CreateFormErrorLabel.IsVisible = true;
                return;
            }

            CreateTaskOverlay.IsVisible = false;
            await DisplayAlertAsync(
                "Created",
                string.IsNullOrWhiteSpace(message) ? "Employee task successfully created." : message,
                "OK");
            _currentPage = 1;
            await LoadTasksAsync();
        }

        private void SetCreateLoading(bool isLoading)
        {
            _isCreating = isLoading;
            CreateSubmitButton.IsEnabled = !isLoading;
            CreateCancelButton.IsEnabled = !isLoading;
            CreateTitleEntry.IsEnabled = !isLoading;
            CreateDescriptionEditor.IsEnabled = !isLoading;
            CreateScheduledDatePicker.IsEnabled = !isLoading;
            CreateScheduledTimePicker.IsEnabled = !isLoading;
            CreateSubmitButton.Text = isLoading ? "Saving..." : "Save Task";
        }
    }
}
