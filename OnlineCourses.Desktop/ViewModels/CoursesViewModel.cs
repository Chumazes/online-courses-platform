using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Windows.Input;
using OnlineCourses.Client.Api;
using OnlineCourses.Desktop.Infrastructure;

namespace OnlineCourses.Desktop.ViewModels;

public sealed class CoursesViewModel : ViewModelBase
{
    private readonly CoursesClient _coursesClient;
    private readonly Action<CourseCardViewModel> _openCourse;
    private readonly RelayCommand _openCourseCommand;
    private readonly RelayCommand _clearSearchCommand;
    private readonly List<CourseCardViewModel> _allCourses;
    private CourseCardViewModel? _selectedCourse;
    private bool _isLoading;
    private string? _errorMessage;
    private string _searchQuery = string.Empty;

    public CoursesViewModel(CoursesClient coursesClient, Action<CourseCardViewModel> openCourse)
    {
        _coursesClient = coursesClient;
        _openCourse = openCourse;

        _openCourseCommand = new RelayCommand(
            _ => OpenSelectedCourse(),
            _ => SelectedCourse is not null && !IsLoading);
        _clearSearchCommand = new RelayCommand(
            _ => SearchQuery = string.Empty,
            _ => !string.IsNullOrWhiteSpace(SearchQuery) && !IsLoading);

        OpenCourseCommand = _openCourseCommand;
        ClearSearchCommand = _clearSearchCommand;
        Courses = new ObservableCollection<CourseCardViewModel>();
        _allCourses = new List<CourseCardViewModel>();
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

    public string SearchQuery
    {
        get => _searchQuery;
        set
        {
            if (SetProperty(ref _searchQuery, value))
            {
                ApplyFilters();
                _clearSearchCommand.RaiseCanExecuteChanged();
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
                _clearSearchCommand.RaiseCanExecuteChanged();
                RaisePropertyChanged(nameof(ShowEmptyState));
                RaisePropertyChanged(nameof(EmptyStateMessage));
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
                RaisePropertyChanged(nameof(EmptyStateMessage));
            }
        }
    }

    public ICommand OpenCourseCommand { get; }
    public ICommand ClearSearchCommand { get; }

    public bool ShowEmptyState =>
        !IsLoading &&
        string.IsNullOrWhiteSpace(ErrorMessage) &&
        Courses.Count == 0;

    public string EmptyStateMessage =>
        _allCourses.Count == 0
            ? "Курсы пока не добавлены."
            : "По вашему запросу ничего не найдено.";

    public async Task LoadCoursesAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
        Courses.Clear();
        _allCourses.Clear();
        SelectedCourse = null;
        RaisePropertyChanged(nameof(ShowEmptyState));
        RaisePropertyChanged(nameof(EmptyStateMessage));

        try
        {
            var response = await _coursesClient.GetAllAsync(pageNumber: 1, pageSize: 20, all: false);

            foreach (var dto in response.Items)
            {
                _allCourses.Add(new CourseCardViewModel
                {
                    Id = dto.CourseId,
                    Title = dto.Title,
                    Description = dto.Description,
                    Level = dto.Level,
                    Price = dto.Price
                });
            }

            ApplyFilters();
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

    private void ApplyFilters()
    {
        var selectedCourseId = SelectedCourse?.Id;
        IEnumerable<CourseCardViewModel> filteredCourses = _allCourses;

        if (!string.IsNullOrWhiteSpace(SearchQuery))
        {
            var query = SearchQuery.Trim();

            filteredCourses = filteredCourses.Where(course =>
                course.Title.Contains(query, StringComparison.CurrentCultureIgnoreCase) ||
                course.Description.Contains(query, StringComparison.CurrentCultureIgnoreCase) ||
                course.Level.Contains(query, StringComparison.CurrentCultureIgnoreCase));
        }

        var filteredList = filteredCourses.ToList();
        Courses.Clear();

        foreach (var course in filteredList)
        {
            Courses.Add(course);
        }

        if (selectedCourseId is not null)
        {
            SelectedCourse = Courses.FirstOrDefault(course => course.Id == selectedCourseId.Value);
        }

        RaisePropertyChanged(nameof(ShowEmptyState));
        RaisePropertyChanged(nameof(EmptyStateMessage));
        _openCourseCommand.RaiseCanExecuteChanged();
    }

    private void OpenSelectedCourse()
    {
        if (SelectedCourse is not null)
        {
            _openCourse(SelectedCourse);
        }
    }
}
