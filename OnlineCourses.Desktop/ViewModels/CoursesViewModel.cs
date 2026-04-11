using System.Collections.ObjectModel;
using System.Net.Http;
using System.Windows.Input;
using OnlineCourses.Client.Api;
using OnlineCourses.Desktop.Infrastructure;
using OnlineCourses.Models.DTOs;

namespace OnlineCourses.Desktop.ViewModels;

public sealed class CoursesViewModel : ViewModelBase
{
    private const int PageSize = 6;

    private readonly CoursesClient _coursesClient;
    private readonly Action<CourseCardViewModel> _openCourse;
    private readonly RelayCommand _openCourseCommand;
    private readonly RelayCommand _clearSearchCommand;
    private readonly RelayCommand _previousPageCommand;
    private readonly RelayCommand _nextPageCommand;
    private CourseCardViewModel? _selectedCourse;
    private CourseCategoryDto? _selectedCategory;
    private FilterOptionViewModel? _selectedLevel;
    private FilterOptionViewModel? _selectedSort;
    private bool _isLoading;
    private bool _isInitializingFilters;
    private bool _reloadRequested;
    private bool _resetPageOnReload;
    private bool _categoriesLoaded;
    private string? _errorMessage;
    private string _searchQuery = string.Empty;
    private int _currentPage = 1;
    private int _totalPages = 1;
    private int _totalCount;

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
        _previousPageCommand = new RelayCommand(
            _ => ChangePage(-1),
            _ => CanGoToPreviousPage);
        _nextPageCommand = new RelayCommand(
            _ => ChangePage(1),
            _ => CanGoToNextPage);

        OpenCourseCommand = _openCourseCommand;
        ClearSearchCommand = _clearSearchCommand;
        PreviousPageCommand = _previousPageCommand;
        NextPageCommand = _nextPageCommand;

        Courses = new ObservableCollection<CourseCardViewModel>();
        Categories = new ObservableCollection<CourseCategoryDto>();
        LevelOptions = new[]
        {
            new FilterOptionViewModel { Value = "all", Label = "Все уровни" },
            new FilterOptionViewModel { Value = "beginner", Label = "Beginner" },
            new FilterOptionViewModel { Value = "intermediate", Label = "Intermediate" },
            new FilterOptionViewModel { Value = "advanced", Label = "Advanced" }
        };
        SortOptions = new[]
        {
            new FilterOptionViewModel { Value = "created_desc", Label = "Сначала новые" },
            new FilterOptionViewModel { Value = "title_asc", Label = "По названию" },
            new FilterOptionViewModel { Value = "price_asc", Label = "Сначала дешевле" },
            new FilterOptionViewModel { Value = "price_desc", Label = "Сначала дороже" },
            new FilterOptionViewModel { Value = "rating_desc", Label = "По рейтингу" }
        };

        _selectedLevel = LevelOptions[0];
        _selectedSort = SortOptions[0];
    }

    public ObservableCollection<CourseCardViewModel> Courses { get; }

    public ObservableCollection<CourseCategoryDto> Categories { get; }

    public IReadOnlyList<FilterOptionViewModel> LevelOptions { get; }

    public IReadOnlyList<FilterOptionViewModel> SortOptions { get; }

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
                RequestReload(resetPage: true);
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
                RequestReload(resetPage: true);
            }
        }
    }

    public FilterOptionViewModel? SelectedSort
    {
        get => _selectedSort;
        set
        {
            if (SetProperty(ref _selectedSort, value))
            {
                RequestReload(resetPage: true);
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
                RequestReload(resetPage: true);
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
                _previousPageCommand.RaiseCanExecuteChanged();
                _nextPageCommand.RaiseCanExecuteChanged();
                RaisePropertyChanged(nameof(ShowEmptyState));
                RaisePropertyChanged(nameof(EmptyStateMessage));
                RaisePropertyChanged(nameof(CanGoToPreviousPage));
                RaisePropertyChanged(nameof(CanGoToNextPage));
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

    public ICommand PreviousPageCommand { get; }

    public ICommand NextPageCommand { get; }

    public int CurrentPage
    {
        get => _currentPage;
        private set
        {
            if (SetProperty(ref _currentPage, value))
            {
                RaisePropertyChanged(nameof(PageCaption));
                RaisePropertyChanged(nameof(PaginationHint));
                RaisePropertyChanged(nameof(ResultsCaption));
                RaisePropertyChanged(nameof(CanGoToPreviousPage));
                RaisePropertyChanged(nameof(CanGoToNextPage));
                _previousPageCommand.RaiseCanExecuteChanged();
                _nextPageCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public int TotalPages
    {
        get => _totalPages;
        private set
        {
            if (SetProperty(ref _totalPages, value))
            {
                RaisePropertyChanged(nameof(PageCaption));
                RaisePropertyChanged(nameof(PaginationHint));
                RaisePropertyChanged(nameof(ResultsCaption));
                RaisePropertyChanged(nameof(CanGoToNextPage));
                _nextPageCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public int TotalCount
    {
        get => _totalCount;
        private set
        {
            if (SetProperty(ref _totalCount, value))
            {
                RaisePropertyChanged(nameof(PageCaption));
                RaisePropertyChanged(nameof(PaginationHint));
                RaisePropertyChanged(nameof(ResultsCaption));
            }
        }
    }

    public bool HasActiveFilters =>
        !string.IsNullOrWhiteSpace(SearchQuery) ||
        (SelectedCategory?.CategoryId ?? 0) > 0 ||
        !string.Equals(SelectedLevel?.Value, "all", StringComparison.OrdinalIgnoreCase);

    public bool ShowEmptyState =>
        !IsLoading &&
        string.IsNullOrWhiteSpace(ErrorMessage) &&
        Courses.Count == 0;

    public string EmptyStateMessage =>
        HasActiveFilters
            ? "По вашим фильтрам ничего не найдено."
            : "Курсы пока не добавлены.";

    public string ResultsCaption =>
        TotalCount == 0
            ? "Курсов пока нет."
            : $"Найдено курсов: {TotalCount}";

    public string PageCaption => $"Страница {CurrentPage} из {TotalPages}";

    public string PaginationHint =>
        TotalCount == 0
            ? "Когда в каталоге появятся курсы, здесь отобразится навигация."
            : TotalPages <= 1
                ? "Сейчас весь каталог помещается на одной странице."
                : "Используй кнопки «Предыдущая» и «Следующая», чтобы листать каталог.";

    public bool CanGoToPreviousPage => !IsLoading && CurrentPage > 1;

    public bool CanGoToNextPage => !IsLoading && CurrentPage < TotalPages;

    public async Task LoadCoursesAsync()
    {
        await LoadCategoriesAsync();

        if (string.IsNullOrWhiteSpace(ErrorMessage))
        {
            await LoadCoursesPageAsync(resetPage: false);
        }
    }

    private async Task LoadCategoriesAsync()
    {
        var selectedCategoryId = ResolveCategoryId();
        IsLoading = true;
        ErrorMessage = null;
        Categories.Clear();

        try
        {
            Categories.Add(new CourseCategoryDto
            {
                CategoryId = 0,
                Name = "Все категории"
            });

            foreach (var category in (await _coursesClient.GetCategoriesAsync()).OrderBy(item => item.Name))
            {
                Categories.Add(category);
            }

            _isInitializingFilters = true;
            SelectedCategory = Categories.FirstOrDefault(item => item.CategoryId == selectedCategoryId) ?? Categories.FirstOrDefault();

            if (!_categoriesLoaded)
            {
                SelectedLevel = LevelOptions[0];
                SelectedSort = SortOptions[0];
            }

            _categoriesLoaded = true;
        }
        catch (ApiException ex)
        {
            ErrorMessage = GetFriendlyApiError(ex, "Не удалось загрузить категории курсов.");
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
            _isInitializingFilters = false;
            IsLoading = false;
        }
    }

    private void ClearFilters()
    {
        _isInitializingFilters = true;
        SearchQuery = string.Empty;
        SelectedCategory = Categories.FirstOrDefault();
        SelectedLevel = LevelOptions[0];
        SelectedSort = SortOptions[0];
        _isInitializingFilters = false;
        RequestReload(resetPage: true);
    }

    private void OpenSelectedCourse()
    {
        if (SelectedCourse is not null)
        {
            _openCourse(SelectedCourse);
        }
    }

    private void ChangePage(int delta)
    {
        if (delta < 0 && !CanGoToPreviousPage)
        {
            return;
        }

        if (delta > 0 && !CanGoToNextPage)
        {
            return;
        }

        CurrentPage += delta;
        RequestReload(resetPage: false);
    }

    private void RequestReload(bool resetPage)
    {
        if (_isInitializingFilters || !_categoriesLoaded)
        {
            return;
        }

        RaisePropertyChanged(nameof(HasActiveFilters));
        _clearSearchCommand.RaiseCanExecuteChanged();

        if (IsLoading)
        {
            _reloadRequested = true;
            _resetPageOnReload |= resetPage;
            return;
        }

        _ = LoadCoursesPageAsync(resetPage);
    }

    private async Task LoadCoursesPageAsync(bool resetPage)
    {
        if (resetPage)
        {
            CurrentPage = 1;
        }

        IsLoading = true;
        ErrorMessage = null;
        Courses.Clear();
        SelectedCourse = null;

        try
        {
            var (sortBy, sortOrder) = ResolveSort();
            var page = await _coursesClient.GetAllAsync(
                pageNumber: CurrentPage,
                pageSize: PageSize,
                all: false,
                level: ResolveLevel(),
                categoryId: ResolveCategoryId(),
                search: ResolveSearchQuery(),
                sortBy: sortBy,
                sortOrder: sortOrder);

            TotalCount = page.TotalCount;
            TotalPages = Math.Max(1, page.TotalPages);

            foreach (var dto in page.Items)
            {
                Courses.Add(MapCourse(dto));
            }
        }
        catch (ApiException ex)
        {
            ErrorMessage = GetFriendlyApiError(ex, "Не удалось загрузить каталог курсов.");
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
            RaisePropertyChanged(nameof(PageCaption));
            RaisePropertyChanged(nameof(PaginationHint));
            RaisePropertyChanged(nameof(ResultsCaption));
            RaisePropertyChanged(nameof(ShowEmptyState));
            RaisePropertyChanged(nameof(EmptyStateMessage));
            _clearSearchCommand.RaiseCanExecuteChanged();
            _openCourseCommand.RaiseCanExecuteChanged();

            if (_reloadRequested)
            {
                var pendingReset = _resetPageOnReload;
                _reloadRequested = false;
                _resetPageOnReload = false;
                _ = LoadCoursesPageAsync(pendingReset);
            }
        }
    }

    private string? ResolveSearchQuery()
    {
        return string.IsNullOrWhiteSpace(SearchQuery) ? null : SearchQuery.Trim();
    }

    private int? ResolveCategoryId()
    {
        return (SelectedCategory?.CategoryId ?? 0) > 0 ? SelectedCategory!.CategoryId : null;
    }

    private string? ResolveLevel()
    {
        return string.Equals(SelectedLevel?.Value, "all", StringComparison.OrdinalIgnoreCase)
            ? null
            : SelectedLevel?.Value;
    }

    private (string sortBy, string sortOrder) ResolveSort()
    {
        return SelectedSort?.Value switch
        {
            "title_asc" => ("title", "asc"),
            "price_asc" => ("price", "asc"),
            "price_desc" => ("price", "desc"),
            "rating_desc" => ("rating", "desc"),
            _ => ("createdAt", "desc")
        };
    }

    private static CourseCardViewModel MapCourse(CourseResponseDto dto)
    {
        return new CourseCardViewModel
        {
            Id = dto.CourseId,
            Title = dto.Title,
            Description = dto.Description,
            CategoryId = dto.CategoryId,
            CategoryName = dto.CategoryName ?? "Без категории",
            Level = dto.Level,
            Price = dto.Price
        };
    }
}
