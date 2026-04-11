using System.Collections.ObjectModel;
using System.Net.Http;
using OnlineCourses.Client.Api;
using OnlineCourses.Models.DTOs;

namespace OnlineCourses.Desktop.ViewModels;

public sealed class RoleDashboardViewModel : ViewModelBase
{
    private readonly CoursesClient _coursesClient;
    private readonly bool _showAllCourses;
    private readonly bool _isAdmin;
    private bool _isLoading;
    private string? _errorMessage;
    private int _totalCourses;
    private int _publishedCourses;
    private int _draftCourses;
    private int _totalStudents;
    private int _attentionCourses;
    private double _averageRating;
    private string _focusTitle = "Панель готова к работе";
    private string _focusDescription = "Здесь будет собираться сводка по курсам, студентам и качеству контента.";
    private string _focusAction = "Открой управление курсами, чтобы продолжить работу.";

    public RoleDashboardViewModel(
        CoursesClient coursesClient,
        bool showAllCourses,
        bool isAdmin)
    {
        _coursesClient = coursesClient;
        _showAllCourses = showAllCourses;
        _isAdmin = isAdmin;
        Courses = new ObservableCollection<RoleDashboardCourseViewModel>();
    }

    public ObservableCollection<RoleDashboardCourseViewModel> Courses { get; }

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

    public int TotalCourses
    {
        get => _totalCourses;
        private set => SetProperty(ref _totalCourses, value);
    }

    public int PublishedCourses
    {
        get => _publishedCourses;
        private set => SetProperty(ref _publishedCourses, value);
    }

    public int DraftCourses
    {
        get => _draftCourses;
        private set => SetProperty(ref _draftCourses, value);
    }

    public int TotalStudents
    {
        get => _totalStudents;
        private set => SetProperty(ref _totalStudents, value);
    }

    public int AttentionCourses
    {
        get => _attentionCourses;
        private set => SetProperty(ref _attentionCourses, value);
    }

    public double AverageRating
    {
        get => _averageRating;
        private set
        {
            if (SetProperty(ref _averageRating, value))
            {
                RaisePropertyChanged(nameof(AverageRatingCaption));
            }
        }
    }

    public string FocusTitle
    {
        get => _focusTitle;
        private set => SetProperty(ref _focusTitle, value);
    }

    public string FocusDescription
    {
        get => _focusDescription;
        private set => SetProperty(ref _focusDescription, value);
    }

    public string FocusAction
    {
        get => _focusAction;
        private set => SetProperty(ref _focusAction, value);
    }

    public string Title => _isAdmin ? "Панель администратора" : "Панель преподавателя";

    public string Subtitle =>
        _isAdmin
            ? "Быстрый обзор по всем курсам, студентам и тем местам, где платформе уже нужна реакция."
            : "Сводка по твоим курсам, студентам и следующим действиям без лишних переходов между экранами.";

    public string AverageRatingCaption => AverageRating <= 0 ? "Без оценок" : $"{AverageRating:0.0} / 5";

    public bool ShowEmptyState =>
        !IsLoading &&
        string.IsNullOrWhiteSpace(ErrorMessage) &&
        Courses.Count == 0;

    public async Task LoadAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
        Courses.Clear();

        try
        {
            var courses = await LoadCoursesAsync();
            var orderedCourses = courses
                .OrderByDescending(course => course.TotalStudents)
                .ThenByDescending(course => course.AvgRating)
                .ThenByDescending(course => course.CreatedAt)
                .ToList();

            foreach (var course in orderedCourses)
            {
                Courses.Add(new RoleDashboardCourseViewModel
                {
                    CourseId = course.CourseId,
                    Title = course.Title,
                    Description = course.Description,
                    AuthorName = course.AuthorName,
                    Level = course.Level,
                    Status = course.Status,
                    Price = course.Price,
                    AvgRating = course.AvgRating,
                    TotalStudents = course.TotalStudents,
                    CategoryId = course.CategoryId,
                    CategoryName = course.CategoryName,
                    CoverImageUrl = course.CoverImageUrl,
                    CreatedAt = course.CreatedAt,
                    ShowModerationAction = _isAdmin
                });
            }

            TotalCourses = orderedCourses.Count;
            PublishedCourses = orderedCourses.Count(course => course.Status.Equals("published", StringComparison.OrdinalIgnoreCase));
            DraftCourses = orderedCourses.Count(course => course.Status.Equals("draft", StringComparison.OrdinalIgnoreCase));
            TotalStudents = orderedCourses.Sum(course => course.TotalStudents);
            AttentionCourses = orderedCourses.Count(course =>
                course.Status.Equals("draft", StringComparison.OrdinalIgnoreCase) ||
                (course.Status.Equals("published", StringComparison.OrdinalIgnoreCase) && course.TotalStudents == 0));

            var ratedCourses = orderedCourses.Where(course => course.AvgRating > 0).ToList();
            AverageRating = ratedCourses.Count == 0
                ? 0
                : Math.Round(ratedCourses.Average(course => (double)course.AvgRating), 2);

            BuildFocusBlock(orderedCourses);
        }
        catch (ApiException ex)
        {
            ErrorMessage = GetFriendlyApiError(ex, "Не удалось загрузить панель преподавателя.");
        }
        catch (HttpRequestException)
        {
            ErrorMessage = GetFriendlyConnectionError("Не удалось загрузить панель преподавателя. Проверь, доступен ли API.");
        }
        catch (Exception ex)
        {
            ErrorMessage = GetFriendlyUnexpectedError(ex, "Не удалось загрузить панель преподавателя.");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void BuildFocusBlock(IReadOnlyList<CourseResponseDto> courses)
    {
        var draftCourse = courses.FirstOrDefault(course => course.Status.Equals("draft", StringComparison.OrdinalIgnoreCase));
        if (draftCourse is not null)
        {
            FocusTitle = $"Нужен фокус на курсе «{draftCourse.Title}»";
            FocusDescription = "В панели есть черновик. Его стоит проверить первым, чтобы не оставлять курс вне витрины и рабочего сценария.";
            FocusAction = "Следующий шаг: открой управление курсами и доведи черновик до публикации.";
            return;
        }

        var noStudentsCourse = courses.FirstOrDefault(course =>
            course.Status.Equals("published", StringComparison.OrdinalIgnoreCase) &&
            course.TotalStudents == 0);
        if (noStudentsCourse is not null)
        {
            FocusTitle = $"Пора усилить курс «{noStudentsCourse.Title}»";
            FocusDescription = "Курс уже опубликован, но у него пока нет студентов. Стоит посмотреть структуру, описание и позиционирование в каталоге.";
            FocusAction = "Следующий шаг: открой аналитику или управление курсом и проверь, как его можно сделать понятнее для студента.";
            return;
        }

        var unratedCourse = courses.FirstOrDefault(course => course.TotalStudents > 0 && course.AvgRating <= 0);
        if (unratedCourse is not null)
        {
            FocusTitle = $"У курса «{unratedCourse.Title}» уже есть студенты";
            FocusDescription = "Прогресс по курсу уже идёт, но отзывов пока нет. Это хороший кандидат, чтобы собрать первое качество обратной связи.";
            FocusAction = "Следующий шаг: открой студентов курса и проверь, как идёт обучение, а затем смотри отзывы и аналитику.";
            return;
        }

        var strongestCourse = courses
            .OrderByDescending(course => course.TotalStudents)
            .ThenByDescending(course => course.AvgRating)
            .FirstOrDefault();

        if (strongestCourse is not null)
        {
            FocusTitle = $"Сильнее всего сейчас выглядит «{strongestCourse.Title}»";
            FocusDescription = "Этот курс лучше остальных держит внимание: у него хороший набор студентов и понятное место в общей картине платформы.";
            FocusAction = "Следующий шаг: используй быстрые действия по карточкам ниже, чтобы сразу перейти к студентам, аналитике или отзывам.";
            return;
        }

        FocusTitle = "Панель готова к работе";
        FocusDescription = "Как только появятся курсы и студенты, здесь сразу будет видно, что именно требует внимания в первую очередь.";
        FocusAction = "Следующий шаг: открой управление курсами и начни наполнять платформу.";
    }

    private async Task<IReadOnlyList<CourseResponseDto>> LoadCoursesAsync()
    {
        if (!_showAllCourses)
        {
            return await _coursesClient.GetMyAsync();
        }

        var pageNumber = 1;
        var courses = new List<CourseResponseDto>();

        while (true)
        {
            var page = await _coursesClient.GetAllAsync(pageNumber: pageNumber, pageSize: 100, all: true);
            if (page.Items is { Count: > 0 })
            {
                courses.AddRange(page.Items);
            }

            if (page.TotalPages <= pageNumber || page.Items.Count == 0)
            {
                break;
            }

            pageNumber++;
        }

        return courses;
    }
}
