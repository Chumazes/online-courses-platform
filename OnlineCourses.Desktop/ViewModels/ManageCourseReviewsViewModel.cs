using System.Collections.ObjectModel;
using System.Net.Http;
using System.Text.Json;
using OnlineCourses.Client.Api;

namespace OnlineCourses.Desktop.ViewModels;

public sealed class ManageCourseReviewsViewModel : ViewModelBase
{
    private readonly ReviewsClient _reviewsClient;
    private readonly bool _canModerate;
    private ReviewItemViewModel? _selectedReview;
    private bool _isLoading;
    private bool _isProcessing;
    private double _averageRating;
    private int _totalReviews;
    private string? _statusMessage;
    private string? _errorMessage;

    public ManageCourseReviewsViewModel(ManageCourseItemViewModel course, ReviewsClient reviewsClient, bool canModerate)
    {
        Course = course;
        _reviewsClient = reviewsClient;
        _canModerate = canModerate;
        Reviews = new ObservableCollection<ReviewItemViewModel>();
    }

    public ManageCourseItemViewModel Course { get; }

    public ObservableCollection<ReviewItemViewModel> Reviews { get; }

    public ReviewItemViewModel? SelectedReview
    {
        get => _selectedReview;
        set
        {
            if (SetProperty(ref _selectedReview, value))
            {
                RaisePropertyChanged(nameof(HasSelectedReview));
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
                RaisePropertyChanged(nameof(ShowEmptyState));
            }
        }
    }

    public bool IsProcessing
    {
        get => _isProcessing;
        private set => SetProperty(ref _isProcessing, value);
    }

    public bool CanModerate => _canModerate;

    public bool HasSelectedReview => SelectedReview is not null;

    public bool ShowEmptyState =>
        !IsLoading &&
        string.IsNullOrWhiteSpace(ErrorMessage) &&
        Reviews.Count == 0;

    public double AverageRating
    {
        get => _averageRating;
        private set
        {
            if (SetProperty(ref _averageRating, value))
            {
                RaisePropertyChanged(nameof(RatingSummaryText));
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
                RaisePropertyChanged(nameof(RatingSummaryText));
            }
        }
    }

    public string RatingSummaryText =>
        TotalReviews == 0
            ? "Пока нет отзывов"
            : $"Средняя оценка {AverageRating:0.0} из 5";

    public string? StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
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

    public async Task LoadAsync()
    {
        IsLoading = true;
        StatusMessage = null;
        ErrorMessage = null;
        Reviews.Clear();

        try
        {
            var ratingTask = _reviewsClient.GetCourseRatingAsync(Course.CourseId);
            var reviewsTask = _canModerate
                ? _reviewsClient.GetCourseReviewsForModerationAsync(Course.CourseId)
                : _reviewsClient.GetCourseReviewsAsync(Course.CourseId);

            await Task.WhenAll(ratingTask, reviewsTask);

            var rating = await ratingTask;
            AverageRating = rating.AverageRating;
            TotalReviews = rating.TotalReviews;

            foreach (var review in (await reviewsTask).OrderByDescending(item => item.ReviewDate))
            {
                Reviews.Add(new ReviewItemViewModel
                {
                    ReviewId = review.ReviewId,
                    UserId = review.UserId,
                    UserName = string.IsNullOrWhiteSpace(review.UserName) ? "Пользователь" : review.UserName,
                    Rating = review.Rating,
                    Comment = review.Comment,
                    ReviewDate = review.ReviewDate,
                    IsApproved = review.IsApproved
                });
            }

            SelectedReview = Reviews.FirstOrDefault();
        }
        catch (ApiException ex)
        {
            ErrorMessage = GetFriendlyApiError(ex, "Не удалось загрузить отзывы курса.");
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "Не удалось загрузить отзывы курса. Проверь, доступен ли API.";
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

    public async Task ApproveSelectedAsync(bool approve)
    {
        if (!_canModerate || SelectedReview is null)
        {
            return;
        }

        IsProcessing = true;
        StatusMessage = null;
        ErrorMessage = null;

        try
        {
            await _reviewsClient.ApproveAsync(SelectedReview.ReviewId, approve);
            StatusMessage = approve ? "Отзыв опубликован." : "Отзыв отправлен на доработку.";
            await LoadAsync();
        }
        catch (ApiException ex)
        {
            ErrorMessage = GetFriendlyApiError(ex, "Не удалось изменить статус отзыва.");
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "Не удалось изменить статус отзыва. Проверь, доступен ли API.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsProcessing = false;
        }
    }

    public async Task DeleteSelectedAsync()
    {
        if (!_canModerate || SelectedReview is null)
        {
            return;
        }

        IsProcessing = true;
        StatusMessage = null;
        ErrorMessage = null;

        try
        {
            await _reviewsClient.DeleteAsync(SelectedReview.ReviewId);
            StatusMessage = "Отзыв удалён.";
            await LoadAsync();
        }
        catch (ApiException ex)
        {
            ErrorMessage = GetFriendlyApiError(ex, "Не удалось удалить отзыв.");
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "Не удалось удалить отзыв. Проверь, доступен ли API.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsProcessing = false;
        }
    }

    private static string ExtractApiErrorMessage(ApiException ex)
    {
        if (string.IsNullOrWhiteSpace(ex.ResponseBody))
        {
            return ex.Message;
        }

        try
        {
            using var document = JsonDocument.Parse(ex.ResponseBody);
            if (document.RootElement.ValueKind == JsonValueKind.Object &&
                document.RootElement.TryGetProperty("message", out var messageElement))
            {
                var message = messageElement.GetString();
                if (!string.IsNullOrWhiteSpace(message))
                {
                    return message;
                }
            }
        }
        catch
        {
        }

        return ex.Message;
    }
}
