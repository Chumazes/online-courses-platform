using System.Collections.ObjectModel;
using System.Net.Http;
using OnlineCourses.Client.Api;
using OnlineCourses.Desktop.Infrastructure;

namespace OnlineCourses.Desktop.ViewModels;

public sealed class ManageCourseStudentsViewModel : ViewModelBase
{
    private readonly EnrollmentsClient _enrollmentsClient;
    private readonly FilesClient _filesClient;
    private bool _isLoading;
    private string? _errorMessage;

    public ManageCourseStudentsViewModel(
        ManageCourseItemViewModel course,
        EnrollmentsClient enrollmentsClient,
        FilesClient filesClient)
    {
        Course = course;
        _enrollmentsClient = enrollmentsClient;
        _filesClient = filesClient;
        Students = new ObservableCollection<CourseEnrollmentItemViewModel>();
    }

    public ManageCourseItemViewModel Course { get; }

    public ObservableCollection<CourseEnrollmentItemViewModel> Students { get; }

    public bool IsLoading
    {
        get => _isLoading;
        private set
        {
            if (SetProperty(ref _isLoading, value))
            {
                RaisePropertyChanged(nameof(ShowEmptyState));
            }
        }
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set
        {
            if (SetProperty(ref _errorMessage, value))
            {
                RaisePropertyChanged(nameof(ShowEmptyState));
            }
        }
    }

    public bool ShowEmptyState =>
        !IsLoading &&
        string.IsNullOrWhiteSpace(ErrorMessage) &&
        Students.Count == 0;

    public async Task LoadAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
        Students.Clear();

        try
        {
            var enrollments = await _enrollmentsClient.GetCourseEnrollmentsAsync(Course.CourseId);
            foreach (var enrollment in enrollments.OrderByDescending(item => item.EnrollmentDate))
            {
                Students.Add(new CourseEnrollmentItemViewModel
                {
                    EnrollmentId = enrollment.EnrollmentId,
                    UserId = enrollment.UserId,
                    AvatarSource = ImageSourceFactory.Create(_filesClient.BuildDownloadUrl(enrollment.UserAvatarUrl)),
                    UserName = string.IsNullOrWhiteSpace(enrollment.UserName) ? "Студент" : enrollment.UserName,
                    EnrollmentDate = enrollment.EnrollmentDate,
                    Status = enrollment.Status,
                    OverallProgress = enrollment.OverallProgress
                });
            }
        }
        catch (ApiException ex)
        {
            ErrorMessage = GetFriendlyApiError(ex, "Не удалось загрузить студентов курса.");
        }
        catch (HttpRequestException)
        {
            ErrorMessage = GetFriendlyConnectionError("Не удалось загрузить студентов курса. Проверь, доступен ли API.");
        }
        catch (Exception ex)
        {
            ErrorMessage = GetFriendlyUnexpectedError(ex, "Не удалось загрузить студентов курса.");
        }
        finally
        {
            IsLoading = false;
        }
    }
}
