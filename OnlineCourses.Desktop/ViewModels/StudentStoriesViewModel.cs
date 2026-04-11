using System.Collections.ObjectModel;
using System.Net.Http;
using OnlineCourses.Client.Api;
using OnlineCourses.Desktop.Infrastructure;

namespace OnlineCourses.Desktop.ViewModels;

public sealed class StudentStoriesViewModel : ViewModelBase
{
    private readonly CoursesClient _coursesClient;
    private readonly ReviewsClient _reviewsClient;
    private readonly FilesClient _filesClient;
    private bool _isLoading;
    private string? _errorMessage;
    private int _storiesCount;
    private int _reviewedCoursesCount;
    private string _averageRatingCaption = "Пока без оценок";

    public StudentStoriesViewModel(
        CoursesClient coursesClient,
        ReviewsClient reviewsClient,
        FilesClient filesClient)
    {
        _coursesClient = coursesClient;
        _reviewsClient = reviewsClient;
        _filesClient = filesClient;
        Stories = new ObservableCollection<PublicStudentStoryViewModel>();
    }

    public ObservableCollection<PublicStudentStoryViewModel> Stories { get; }

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
        private set => SetProperty(ref _errorMessage, value);
    }

    public int StoriesCount
    {
        get => _storiesCount;
        private set => SetProperty(ref _storiesCount, value);
    }

    public int ReviewedCoursesCount
    {
        get => _reviewedCoursesCount;
        private set => SetProperty(ref _reviewedCoursesCount, value);
    }

    public string AverageRatingCaption
    {
        get => _averageRatingCaption;
        private set => SetProperty(ref _averageRatingCaption, value);
    }

    public bool ShowEmptyState => !IsLoading && string.IsNullOrWhiteSpace(ErrorMessage) && Stories.Count == 0;

    public async Task LoadAsync()
    {
        if (IsLoading)
        {
            return;
        }

        IsLoading = true;
        ErrorMessage = null;
        Stories.Clear();
        StoriesCount = 0;
        ReviewedCoursesCount = 0;
        AverageRatingCaption = "Пока без оценок";

        try
        {
            var coursePage = await _coursesClient.GetAllAsync(pageNumber: 1, pageSize: 6, all: false);
            var courses = coursePage.Items.Take(6).ToList();

            var reviewTasks = courses
                .Select(course => LoadCourseReviewsSafeAsync(course.CourseId))
                .ToArray();

            await Task.WhenAll(reviewTasks);

            var storyItems = courses
                .SelectMany((course, index) => reviewTasks[index].Result.Select(review => new { course, review }))
                .Where(item => item.review.IsApproved)
                .OrderByDescending(item => item.review.ReviewDate)
                .ToList();

            StoriesCount = storyItems.Count;
            ReviewedCoursesCount = storyItems
                .Select(item => item.course.CourseId)
                .Distinct()
                .Count();

            if (storyItems.Count > 0)
            {
                var averageRating = storyItems.Average(item => item.review.Rating);
                AverageRatingCaption = $"{averageRating:0.0} / 5";
            }

            foreach (var item in storyItems.Take(6))
            {
                Stories.Add(new PublicStudentStoryViewModel
                {
                    UserName = item.review.UserName,
                    CourseTitle = item.review.CourseTitle,
                    Comment = item.review.Comment ?? string.Empty,
                    Rating = item.review.Rating,
                    ReviewDate = item.review.ReviewDate,
                    AvatarSource = ImageSourceFactory.Create(_filesClient.BuildDownloadUrl(item.review.UserAvatar))
                });
            }
        }
        catch (ApiException ex)
        {
            ErrorMessage = GetFriendlyApiError(ex, "Не удалось загрузить публичные отзывы студентов.");
        }
        catch (HttpRequestException)
        {
            ErrorMessage = GetFriendlyConnectionError("Не удалось загрузить публичные отзывы студентов. Проверь, доступен ли API.");
        }
        catch (Exception ex)
        {
            ErrorMessage = GetFriendlyUnexpectedError(ex, "Не удалось загрузить публичные отзывы студентов.");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task<IReadOnlyList<OnlineCourses.Models.DTOs.ReviewResponseDto>> LoadCourseReviewsSafeAsync(int courseId)
    {
        try
        {
            return await _reviewsClient.GetCourseReviewsAsync(courseId);
        }
        catch
        {
            return Array.Empty<OnlineCourses.Models.DTOs.ReviewResponseDto>();
        }
    }
}
