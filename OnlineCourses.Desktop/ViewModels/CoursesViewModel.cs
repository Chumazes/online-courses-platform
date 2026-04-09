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
    private readonly RelayCommand _clearSearchCommand;
    private readonly List<CourseCardViewModel> _allCourses;
    private CourseCardViewModel? _selectedCourse;
    private CourseCategoryDto? _selectedCategory;
    private FilterOptionViewModel? _selectedLevel;
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
            _ => ClearFilters(),
            _ => HasActiveFilters && !IsLoading);

        OpenCourseCommand = _openCourseCommand;
        ClearSearchCommand = _clearSearchCommand;
        Courses = new ObservableCollection<CourseCardViewModel>();
        Categories = new ObservableCollection<CourseCategoryDto>();
        LevelOptions = new[]
        {
            new FilterOptionViewModel { Value = "all", Label = "Все уровни" },
            new FilterOptionViewModel { Value = "beginner", Label = "Beginner" },
            new FilterOptionViewModel { Value = "intermediate", Label = "Intermediate" },
            new FilterOptionViewModel { Value = "advanced", Label = "Advanced" }
        };
        _selectedLevel = LevelOptions[0];
        _allCourses = new List<CourseCardViewModel>();
    }

    public ObservableCollection<CourseCardViewModel> Courses { get; }

    public ObservableCollection<CourseCategoryDto> Categories { get; }

    public IReadOnlyList<FilterOptionViewModel> LevelOptions { get; }

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

    public CourseCategoryDto? SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            if (SetProperty(ref _selectedCategory, value))
            {
                ApplyFilters();
                RaisePropertyChanged(nameof(HasActiveFilters));
                _clearSearchCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public FilterOptionViewModel? SelectedLevel
    {
        get => _selectedLevel;
        set
        {
            if (SetProperty(ref _selectedLevel, value))
            {
                ApplyFilters();
                RaisePropertyChanged(nameof(HasActiveFilters));
                _clearSearchCommand.RaiseCanExecuteChanged();
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
                RaisePropertyChanged(nameof(HasActiveFilters));
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

    public bool HasActiveFilters =>
        !string.IsNullOrWhiteSpace(SearchQuery) ||
        (SelectedCategory?.CategoryId ?? 0) > 0 ||
        !string.Equals(SelectedLevel?.Value, "all", StringComparison.OrdinalIgnoreCase);

    public bool ShowEmptyState =>
        !IsLoading &&
        string.IsNullOrWhiteSpace(ErrorMessage) &&
        Courses.Count == 0;

    public string EmptyStateMessage =>
        _allCourses.Count == 0
            ? "Курсы пока не добавлены."
            : "По вашим фильтрам ничего не найдено.";

    public async Task LoadCoursesAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
        Courses.Clear();
        Categories.Clear();
        _allCourses.Clear();
        SelectedCourse = null;
        RaisePropertyChanged(nameof(ShowEmptyState));
        RaisePropertyChanged(nameof(EmptyStateMessage));

        try
        {
            var coursesTask = _coursesClient.GetAllAsync(pageNumber: 1, pageSize: 50, all: false);
            var categoriesTask = _coursesClient.GetCategoriesAsync();
            await Task.WhenAll(coursesTask, categoriesTask);

            Categories.Add(new CourseCategoryDto
            {
                CategoryId = 0,
                Name = "Все категории"
            });

            foreach (var category in (await categoriesTask).OrderBy(item => item.Name))
            {
                Categories.Add(category);
            }

            SelectedCategory = Categories.FirstOrDefault();
            SelectedLevel = LevelOptions[0];

            foreach (var dto in (await coursesTask).Items)
            {
                _allCourses.Add(new CourseCardViewModel
                {
                    Id = dto.CourseId,
                    Title = dto.Title,
                    Description = dto.Description,
                    CategoryId = dto.CategoryId,
                    CategoryName = dto.CategoryName ?? "Без категории",
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
                course.Level.Contains(query, StringComparison.CurrentCultureIgnoreCase) ||
                course.CategoryName.Contains(query, StringComparison.CurrentCultureIgnoreCase));
        }

        if ((SelectedCategory?.CategoryId ?? 0) > 0)
        {
            filteredCourses = filteredCourses.Where(course => course.CategoryId == SelectedCategory!.CategoryId);
        }

        if (!string.Equals(SelectedLevel?.Value, "all", StringComparison.OrdinalIgnoreCase))
        {
            filteredCourses = filteredCourses.Where(course =>
                string.Equals(course.Level, SelectedLevel!.Value, StringComparison.OrdinalIgnoreCase));
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
        RaisePropertyChanged(nameof(HasActiveFilters));
        _openCourseCommand.RaiseCanExecuteChanged();
        _clearSearchCommand.RaiseCanExecuteChanged();
    }

    private void ClearFilters()
    {
        SearchQuery = string.Empty;
        SelectedCategory = Categories.FirstOrDefault();
        SelectedLevel = LevelOptions[0];
        ApplyFilters();
    }

    private void OpenSelectedCourse()
    {
        if (SelectedCourse is not null)
        {
            _openCourse(SelectedCourse);
        }
    }
}
