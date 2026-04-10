using System.Collections.ObjectModel;
using System.Net.Http;
using System.Windows.Input;
using OnlineCourses.Client.Api;
using OnlineCourses.Desktop.Infrastructure;

namespace OnlineCourses.Desktop.ViewModels;

public sealed class MyCoursesViewModel : ViewModelBase
{
    private readonly EnrollmentsClient _enrollmentsClient;
    private readonly Action<CourseCardViewModel> _openCourse;
    private readonly RelayCommand _openCourseCommand;
    private MyCourseCardViewModel? _selectedCourse;
    private bool _isLoading;
    private string? _errorMessage;

    public MyCoursesViewModel(
        EnrollmentsClient enrollmentsClient,
        Action<CourseCardViewModel> openCourse)
    {
        _enrollmentsClient = enrollmentsClient;
        _openCourse = openCourse;
        _openCourseCommand = new RelayCommand(
            _ => OpenSelectedCourse(),
            _ => SelectedCourse is not null && !IsLoading);

        OpenCourseCommand = _openCourseCommand;
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
        Courses.Count == 0;

    public ICommand OpenCourseCommand { get; }

    public async Task LoadAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
        Courses.Clear();
        SelectedCourse = null;
        RaisePropertyChanged(nameof(ShowEmptyState));

        try
        {
            var enrollments = await _enrollmentsClient.GetMyAsync();

            foreach (var enrollment in enrollments
                         .OrderByDescending(item => item.EnrollmentDate))
            {
                Courses.Add(new MyCourseCardViewModel
                {
                    CourseId = enrollment.CourseId,
                    Title = enrollment.CourseTitle,
                    Status = enrollment.Status,
                    OverallProgress = enrollment.OverallProgress,
                    EnrollmentDate = enrollment.EnrollmentDate,
                    CompletedAt = enrollment.CompletedAt
                });
            }
        }
        catch (ApiException ex)
        {
            ErrorMessage = GetFriendlyApiError(ex, "Не удалось загрузить ваши курсы.");
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "Не удалось загрузить ваши курсы. Проверь, доступен ли API.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void OpenSelectedCourse()
    {
        if (SelectedCourse is not null)
        {
            _openCourse(SelectedCourse.ToCourseCard());
        }
    }
}
