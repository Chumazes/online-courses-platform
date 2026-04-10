using System.Collections.ObjectModel;
using System.Net.Http;
using OnlineCourses.Client.Api;
using OnlineCourses.Desktop.Infrastructure;

namespace OnlineCourses.Desktop.ViewModels;

public sealed class ManageCourseAnalyticsViewModel : ViewModelBase
{
    private readonly EnrollmentsClient _enrollmentsClient;
    private readonly ReviewsClient _reviewsClient;
    private readonly FilesClient _filesClient;
    private bool _isLoading;
    private string? _errorMessage;
    private int _studentCount;
    private int _activeStudentsCount;
    private int _completedStudentsCount;
    private int _averageProgress;
    private double _averageRating;
    private int _totalReviews;
    private string _ratingDistributionCaption = "Пока нет оценок.";

    public ManageCourseAnalyticsViewModel(
        ManageCourseItemViewModel course,
        EnrollmentsClient enrollmentsClient,
        ReviewsClient reviewsClient,
        FilesClient filesClient)
    {
        Course = course;
        _enrollmentsClient = enrollmentsClient;
        _reviewsClient = reviewsClient;
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

    public int StudentCount
    {
        get => _studentCount;
        private set => SetProperty(ref _studentCount, value);
    }

    public int ActiveStudentsCount
    {
        get => _activeStudentsCount;
        private set => SetProperty(ref _activeStudentsCount, value);
    }

    public int CompletedStudentsCount
    {
        get => _completedStudentsCount;
        private set => SetProperty(ref _completedStudentsCount, value);
    }

    public int AverageProgress
    {
        get => _averageProgress;
        private set => SetProperty(ref _averageProgress, value);
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

    public int TotalReviews
    {
        get => _totalReviews;
        private set
        {
            if (SetProperty(ref _totalReviews, value))
            {
                RaisePropertyChanged(nameof(ReviewsCaption));
            }
        }
    }

    public string RatingDistributionCaption
    {
        get => _ratingDistributionCaption;
        private set => SetProperty(ref _ratingDistributionCaption, value);
    }

    public bool ShowEmptyState =>
        !IsLoading &&
        string.IsNullOrWhiteSpace(ErrorMessage) &&
        Students.Count == 0;

    public string CompletionRateCaption
    {
        get
        {
            if (StudentCount == 0)
            {
                return "0%";
            }

            var completionRate = (int)Math.Round(CompletedStudentsCount / (double)StudentCount * 100);
            return $"{completionRate}%";
        }
    }

    public string AverageRatingCaption =>
        TotalReviews == 0
            ? "Пока без оценок"
            : $"{AverageRating:0.0} / 5";

    public string ReviewsCaption =>
        TotalReviews == 0
            ? "Отзывов пока нет"
            : $"{TotalReviews} отзыв(ов)";

    public string EngagementCaption =>
        $"Активно учатся: {ActiveStudentsCount} • Завершили: {CompletedStudentsCount}";

    public async Task LoadAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
        Students.Clear();

        try
        {
            var enrollmentsTask = _enrollmentsClient.GetCourseEnrollmentsAsync(Course.CourseId);
            var ratingTask = _reviewsClient.GetCourseRatingAsync(Course.CourseId);

            await Task.WhenAll(enrollmentsTask, ratingTask);

            var enrollments = enrollmentsTask.Result
                .OrderByDescending(item => item.OverallProgress)
                .ThenBy(item => item.UserName)
                .ToList();

            foreach (var enrollment in enrollments)
            {
                Students.Add(new CourseEnrollmentItemViewModel
                {
                    EnrollmentId = enrollment.EnrollmentId,
                    UserId = enrollment.UserId,
                    UserName = string.IsNullOrWhiteSpace(enrollment.UserName) ? "Студент" : enrollment.UserName,
                    AvatarSource = ImageSourceFactory.Create(_filesClient.BuildDownloadUrl(enrollment.UserAvatarUrl)),
                    EnrollmentDate = enrollment.EnrollmentDate,
                    Status = enrollment.Status,
                    OverallProgress = enrollment.OverallProgress
                });
            }

            StudentCount = enrollments.Count;
            ActiveStudentsCount = enrollments.Count(item => item.Status.Equals("active", StringComparison.OrdinalIgnoreCase));
            CompletedStudentsCount = enrollments.Count(item =>
                item.Status.Equals("completed", StringComparison.OrdinalIgnoreCase) || item.OverallProgress >= 100);
            AverageProgress = enrollments.Count == 0
                ? 0
                : (int)Math.Round(enrollments.Average(item => item.OverallProgress));

            var rating = ratingTask.Result;
            AverageRating = rating.AverageRating;
            TotalReviews = rating.TotalReviews;
            RatingDistributionCaption = BuildDistributionCaption(rating.RatingDistribution);

            RaisePropertyChanged(nameof(CompletionRateCaption));
            RaisePropertyChanged(nameof(EngagementCaption));
        }
        catch (ApiException ex)
        {
            ErrorMessage = GetFriendlyApiError(ex, "Не удалось загрузить аналитику курса.");
        }
        catch (HttpRequestException)
        {
            ErrorMessage = GetFriendlyConnectionError("Не удалось загрузить аналитику курса. Проверь, доступен ли API.");
        }
        catch (Exception ex)
        {
            ErrorMessage = GetFriendlyUnexpectedError(ex, "Не удалось загрузить аналитику курса.");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private static string BuildDistributionCaption(IReadOnlyDictionary<int, int>? distribution)
    {
        if (distribution is null || distribution.Count == 0)
        {
            return "Пока нет оценок.";
        }

        var parts = Enumerable
            .Range(1, 5)
            .Reverse()
            .Select(star => $"{star}★: {(distribution.TryGetValue(star, out var count) ? count : 0)}");

        return string.Join("   ", parts);
    }
}
