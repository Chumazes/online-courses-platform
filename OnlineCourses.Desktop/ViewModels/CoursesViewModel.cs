using System.Collections.ObjectModel;
using System.Net.Http;
using System.Windows.Input;
using OnlineCourses.Client.Api;
using OnlineCourses.Desktop.Infrastructure;
using OnlineCourses.Models.DTOs;

namespace OnlineCourses.Desktop.ViewModels;

public sealed class CoursesViewModel : ViewModelBase
{
    private readonly CoursesClient _coursesClient;
    private readonly Action<CourseCardViewModel> _openCourse;
    private readonly RelayCommand _openCourseCommand;
    private CourseCardViewModel? _selectedCourse;
    private bool _isLoading;
    private string? _errorMessage;

    public CoursesViewModel(CoursesClient coursesClient, Action<CourseCardViewModel> openCourse)
    {
        _coursesClient = coursesClient;
        _openCourse = openCourse;

        _openCourseCommand = new RelayCommand(
            _ => OpenSelectedCourse(),
            _ => SelectedCourse is not null && !IsLoading);

        OpenCourseCommand = _openCourseCommand;

        Courses = new ObservableCollection<CourseCardViewModel>();
    }

    public ObservableCollection<CourseCardViewModel> Courses { get; }

    public CourseCardViewModel? SelectedCourse
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
            }
        }
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public ICommand OpenCourseCommand { get; }

    public async Task LoadCoursesAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
        Courses.Clear();

        try
        {
            var response = await _coursesClient.GetAllAsync(pageNumber: 1, pageSize: 20, all: false);

            foreach (var dto in response.Items)
            {
                Courses.Add(new CourseCardViewModel
                {
                    Id = dto.CourseId,
                    Title = dto.Title,
                    Description = dto.Description,
                    Level = dto.Level,
                    Price = dto.Price
                });
            }
        }
        catch (ApiException ex)
        {
            ErrorMessage = ex.ResponseBody ?? ex.Message;
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "Не удалось подключиться к API.";
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
            _openCourse(SelectedCourse);
        }
    }
}
