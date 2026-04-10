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
    private double _averageRating;

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

    public string Title => _isAdmin ? "Панель администратора" : "Панель преподавателя";

    public string Subtitle =>
        _isAdmin
            ? "Общий обзор курсов, публикаций и роста платформы."
            : "Сводка по авторским курсам, студентам и качеству контента.";

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
                    AvgRating = course.AvgRating,
                    TotalStudents = course.TotalStudents,
                    CreatedAt = course.CreatedAt
                });
            }

            TotalCourses = orderedCourses.Count;
            PublishedCourses = orderedCourses.Count(course => course.Status.Equals("published", StringComparison.OrdinalIgnoreCase));
            DraftCourses = orderedCourses.Count(course => course.Status.Equals("draft", StringComparison.OrdinalIgnoreCase));
            TotalStudents = orderedCourses.Sum(course => course.TotalStudents);

            var ratedCourses = orderedCourses.Where(course => course.AvgRating > 0).ToList();
            AverageRating = ratedCourses.Count == 0 ? 0 : Math.Round(ratedCourses.Average(course => (double)course.AvgRating), 2);
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
