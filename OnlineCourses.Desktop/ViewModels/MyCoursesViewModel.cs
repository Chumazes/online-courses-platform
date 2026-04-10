using System.Collections.ObjectModel;
using System.Net.Http;
using System.Windows.Input;
using OnlineCourses.Client.Api;
using OnlineCourses.Desktop.Infrastructure;
using OnlineCourses.Models.DTOs;

namespace OnlineCourses.Desktop.ViewModels;

public sealed class MyCoursesViewModel : ViewModelBase
{
    private readonly EnrollmentsClient _enrollmentsClient;
    private readonly ProgressClient _progressClient;
    private readonly Action<CourseCardViewModel> _openCourse;
    private readonly RelayCommand _openCourseCommand;
    private readonly AsyncRelayCommand _unenrollCommand;
    private MyCourseCardViewModel? _selectedCourse;
    private bool _isLoading;
    private bool _isUnenrolling;
    private string? _errorMessage;
    private string? _statusMessage;

    public MyCoursesViewModel(
        EnrollmentsClient enrollmentsClient,
        ProgressClient progressClient,
        Action<CourseCardViewModel> openCourse)
    {
        _enrollmentsClient = enrollmentsClient;
        _progressClient = progressClient;
        _openCourse = openCourse;
        _openCourseCommand = new RelayCommand(
            _ => OpenSelectedCourse(),
            _ => SelectedCourse is not null && !IsLoading);
        _unenrollCommand = new AsyncRelayCommand(
            UnenrollSelectedAsync,
            () => SelectedCourse is not null && SelectedCourse.CanUnenroll && !IsLoading && !IsUnenrolling);

        OpenCourseCommand = _openCourseCommand;
        UnenrollCommand = _unenrollCommand;
        Courses = new ObservableCollection<MyCourseCardViewModel>();
    }

    public ObservableCollection<MyCourseCardViewModel> Courses { get; }

    public MyCourseCardViewModel? SelectedCourse
    {
        get => _selectedCourse;
        set
        {
            if (SetProperty(ref _selectedCourse, value))
            {
                _openCourseCommand.RaiseCanExecuteChanged();
                _unenrollCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public bool IsLoading
    {
        get => _isLoading;
        private set
        {
            if (SetProperty(ref _isLoading, value))
            {
                _openCourseCommand.RaiseCanExecuteChanged();
                _unenrollCommand.RaiseCanExecuteChanged();
                RaisePropertyChanged(nameof(ShowEmptyState));
            }
        }
    }

    public bool IsUnenrolling
    {
        get => _isUnenrolling;
        private set
        {
            if (SetProperty(ref _isUnenrolling, value))
            {
                RaisePropertyChanged(nameof(UnenrollButtonText));
                _unenrollCommand.RaiseCanExecuteChanged();
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

    public string? StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public bool ShowEmptyState =>
        !IsLoading &&
        string.IsNullOrWhiteSpace(ErrorMessage) &&
        Courses.Count == 0;

    public ICommand OpenCourseCommand { get; }

    public ICommand UnenrollCommand { get; }

    public string UnenrollButtonText => IsUnenrolling ? "Отписываем..." : "Отписаться";

    public async Task LoadAsync(bool preserveStatusMessage = false)
    {
        IsLoading = true;
        ErrorMessage = null;
        if (!preserveStatusMessage)
        {
            StatusMessage = null;
        }

        Courses.Clear();
        SelectedCourse = null;
        RaisePropertyChanged(nameof(ShowEmptyState));

        try
        {
            var enrollmentsTask = _enrollmentsClient.GetMyAsync();
            var progressTask = _progressClient.GetMyAsync();
            await Task.WhenAll(enrollmentsTask, progressTask);

            var progressByCourse = (await progressTask)
                .GroupBy(item => item.CourseId)
                .ToDictionary(group => group.Key, group => SelectBestProgress(group));

            var currentEnrollments = (await enrollmentsTask)
                .GroupBy(item => item.CourseId)
                .Select(group => SelectCurrentEnrollment(group))
                .Where(item => item is not null)
                .OrderByDescending(item => item!.EnrollmentDate)
                .Select(item => item!);

            foreach (var enrollment in currentEnrollments)
            {
                progressByCourse.TryGetValue(enrollment.CourseId, out var courseProgress);

                if (string.Equals(enrollment.Status, "expired", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                Courses.Add(new MyCourseCardViewModel
                {
                    CourseId = enrollment.CourseId,
                    Title = enrollment.CourseTitle,
                    Status = courseProgress?.Status is not null &&
                             !string.Equals(courseProgress.Status, "expired", StringComparison.OrdinalIgnoreCase)
                        ? courseProgress.Status
                        : enrollment.Status,
                    OverallProgress = courseProgress?.OverallProgress ?? enrollment.OverallProgress,
                    TotalLessons = courseProgress?.TotalLessons ?? 0,
                    CompletedLessons = courseProgress?.CompletedLessons ?? 0,
                    EnrollmentDate = enrollment.EnrollmentDate,
                    CompletedAt = courseProgress?.CompletedAt ?? enrollment.CompletedAt
                });
            }
        }
        catch (ApiException ex)
        {
            ErrorMessage = GetFriendlyApiError(ex, "Не удалось загрузить ваши курсы.");
        }
        catch (HttpRequestException)
        {
            ErrorMessage = GetFriendlyConnectionError("Не удалось загрузить ваши курсы. Проверь, доступен ли API.");
        }
        catch (Exception ex)
        {
            ErrorMessage = GetFriendlyUnexpectedError(ex, "Не удалось загрузить ваши курсы.");
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task UnenrollSelectedAsync()
    {
        if (SelectedCourse is null)
        {
            return;
        }

        IsUnenrolling = true;
        ErrorMessage = null;
        StatusMessage = null;

        try
        {
            var courseTitle = SelectedCourse.Title;
            await _enrollmentsClient.UnenrollAsync(SelectedCourse.CourseId);
            StatusMessage = $"Вы отписались от курса \"{courseTitle}\".";
            await LoadAsync(preserveStatusMessage: true);
        }
        catch (ApiException ex)
        {
            ErrorMessage = GetFriendlyApiError(ex, "Не удалось отписаться от курса.");
        }
        catch (HttpRequestException)
        {
            ErrorMessage = GetFriendlyConnectionError("Не удалось отписаться от курса. Проверь, доступен ли API.");
        }
        catch (Exception ex)
        {
            ErrorMessage = GetFriendlyUnexpectedError(ex, "Не удалось отписаться от курса.");
        }
        finally
        {
            IsUnenrolling = false;
        }
    }

    private void OpenSelectedCourse()
    {
        if (SelectedCourse is not null)
        {
            _openCourse(SelectedCourse.ToCourseCard());
        }
    }

    private static EnrollmentResponseDto? SelectCurrentEnrollment(IEnumerable<EnrollmentResponseDto> enrollments)
    {
        return enrollments
            .OrderByDescending(item => GetEnrollmentPriority(item.Status))
            .ThenByDescending(item => item.EnrollmentDate)
            .FirstOrDefault();
    }

    private static CourseProgressResponseDto? SelectBestProgress(IEnumerable<CourseProgressResponseDto> progressItems)
    {
        return progressItems
            .OrderByDescending(item => GetEnrollmentPriority(item.Status))
            .ThenByDescending(item => item.OverallProgress)
            .ThenByDescending(item => item.CompletedAt ?? DateTime.MinValue)
            .FirstOrDefault();
    }

    private static int GetEnrollmentPriority(string? status)
    {
        return status?.ToLowerInvariant() switch
        {
            "completed" => 3,
            "active" => 2,
            "draft" => 1,
            "expired" => 0,
            _ => 1
        };
    }
}
