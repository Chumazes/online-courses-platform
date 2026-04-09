using System.Collections.ObjectModel;
using System.Net;
using System.Net.Http;
using OnlineCourses.Client.Api;
using OnlineCourses.Desktop.Infrastructure;
using OnlineCourses.Models.DTOs;

namespace OnlineCourses.Desktop.ViewModels;

public sealed class CourseDetailsViewModel : ViewModelBase
{
    private readonly int _courseId;
    private readonly CoursesClient _coursesClient;
    private readonly EnrollmentsClient _enrollmentsClient;
    private readonly ProgressClient _progressClient;
    private readonly SectionsClient _sectionsClient;
    private readonly LessonsClient _lessonsClient;
    private readonly ReviewsClient _reviewsClient;
    private readonly AsyncRelayCommand _enrollCommand;
    private readonly AsyncRelayCommand _saveReviewCommand;
    private readonly AsyncRelayCommand _deleteReviewCommand;
    private bool _isLoading;
    private bool _isEnrolling;
    private bool _isEnrolled;
    private bool _isReviewsLoading;
    private bool _isSavingReview;
    private bool _isDeletingReview;
    private bool _currentReviewApproved;
    private int _totalLessons;
    private int _completedLessons;
    private int _overallProgress;
    private int _selectedRating = 5;
    private int _totalReviews;
    private int? _currentReviewId;
    private double _averageRating;
    private string? _errorMessage;
    private string? _reviewsErrorMessage;
    private string? _enrollmentStatusMessage;
    private string? _enrollmentErrorMessage;
    private string? _reviewStatusMessage;
    private string? _reviewErrorMessage;
    private string _reviewComment = string.Empty;
    private string _title;
    private string _description;
    private string _level;
    private string _price;
    private string _authorName;
    private string _categoryName;

    public CourseDetailsViewModel(
        CourseCardViewModel course,
        CoursesClient coursesClient,
        EnrollmentsClient enrollmentsClient,
        ProgressClient progressClient,
        SectionsClient sectionsClient,
        LessonsClient lessonsClient,
        ReviewsClient reviewsClient)
    {
        _courseId = course.Id;
        _coursesClient = coursesClient;
        _enrollmentsClient = enrollmentsClient;
        _progressClient = progressClient;
        _sectionsClient = sectionsClient;
        _lessonsClient = lessonsClient;
        _reviewsClient = reviewsClient;
        _title = course.Title;
        _description = course.Description;
        _level = course.Level;
        _price = FormatPrice(course.Price);
        _authorName = "Автор загружается...";
        _categoryName = "Категория загружается...";
        _enrollCommand = new AsyncRelayCommand(EnrollAsync, () => !IsEnrolled && !IsEnrolling);
        _saveReviewCommand = new AsyncRelayCommand(SaveReviewAsync, () => !IsSavingReview && SelectedRating is >= 1 and <= 5);
        _deleteReviewCommand = new AsyncRelayCommand(DeleteReviewAsync, () => HasOwnReview && !IsDeletingReview);

        Sections = new ObservableCollection<CourseSectionViewModel>();
        Reviews = new ObservableCollection<ReviewItemViewModel>();
        RatingOptions = new[] { 5, 4, 3, 2, 1 };
    }

    public ObservableCollection<CourseSectionViewModel> Sections { get; }

    public ObservableCollection<ReviewItemViewModel> Reviews { get; }

    public IReadOnlyList<int> RatingOptions { get; }

    public string Title
    {
        get => _title;
        private set => SetProperty(ref _title, value);
    }

    public string Description
    {
        get => _description;
        private set => SetProperty(ref _description, value);
    }

    public string Level
    {
        get => _level;
        private set => SetProperty(ref _level, value);
    }

    public string Price
    {
        get => _price;
        private set => SetProperty(ref _price, value);
    }

    public string AuthorName
    {
        get => _authorName;
        private set => SetProperty(ref _authorName, value);
    }

    public string CategoryName
    {
        get => _categoryName;
        private set => SetProperty(ref _categoryName, value);
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

    public bool IsEnrolling
    {
        get => _isEnrolling;
        private set
        {
            if (SetProperty(ref _isEnrolling, value))
            {
                RaisePropertyChanged(nameof(EnrollButtonText));
                _enrollCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public bool IsEnrolled
    {
        get => _isEnrolled;
        private set
        {
            if (SetProperty(ref _isEnrolled, value))
            {
                RaisePropertyChanged(nameof(EnrollButtonText));
                RaisePropertyChanged(nameof(ShowCourseProgress));
                _enrollCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public bool IsReviewsLoading
    {
        get => _isReviewsLoading;
        private set
        {
            if (SetProperty(ref _isReviewsLoading, value))
            {
                RaisePropertyChanged(nameof(ShowReviewsEmptyState));
            }
        }
    }

    public bool IsSavingReview
    {
        get => _isSavingReview;
        private set
        {
            if (SetProperty(ref _isSavingReview, value))
            {
                RaisePropertyChanged(nameof(ReviewButtonText));
                _saveReviewCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public bool IsDeletingReview
    {
        get => _isDeletingReview;
        private set
        {
            if (SetProperty(ref _isDeletingReview, value))
            {
                RaisePropertyChanged(nameof(CanDeleteReview));
                _deleteReviewCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public int TotalLessons
    {
        get => _totalLessons;
        private set
        {
            if (SetProperty(ref _totalLessons, value))
            {
                RaisePropertyChanged(nameof(CourseProgressCaption));
            }
        }
    }

    public int CompletedLessons
    {
        get => _completedLessons;
        private set
        {
            if (SetProperty(ref _completedLessons, value))
            {
                RaisePropertyChanged(nameof(CourseProgressCaption));
            }
        }
    }

    public int OverallProgress
    {
        get => _overallProgress;
        private set => SetProperty(ref _overallProgress, value);
    }

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
                RaisePropertyChanged(nameof(ReviewCountText));
            }
        }
    }

    public int SelectedRating
    {
        get => _selectedRating;
        set
        {
            if (SetProperty(ref _selectedRating, value))
            {
                _saveReviewCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string ReviewComment
    {
        get => _reviewComment;
        set => SetProperty(ref _reviewComment, value);
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

    public string? ReviewsErrorMessage
    {
        get => _reviewsErrorMessage;
        private set
        {
            if (SetProperty(ref _reviewsErrorMessage, value))
            {
                RaisePropertyChanged(nameof(ShowReviewsEmptyState));
            }
        }
    }

    public string? EnrollmentStatusMessage
    {
        get => _enrollmentStatusMessage;
        private set => SetProperty(ref _enrollmentStatusMessage, value);
    }

    public string? EnrollmentErrorMessage
    {
        get => _enrollmentErrorMessage;
        private set => SetProperty(ref _enrollmentErrorMessage, value);
    }

    public string? ReviewStatusMessage
    {
        get => _reviewStatusMessage;
        private set => SetProperty(ref _reviewStatusMessage, value);
    }

    public string? ReviewErrorMessage
    {
        get => _reviewErrorMessage;
        private set => SetProperty(ref _reviewErrorMessage, value);
    }

    public string EnrollButtonText =>
        IsEnrolled ? "Вы записаны" : IsEnrolling ? "Записываем..." : "Записаться на курс";

    public string CourseProgressCaption => $"{CompletedLessons} из {TotalLessons} уроков завершено";

    public bool ShowCourseProgress => IsEnrolled && TotalLessons > 0;

    public string RatingSummaryText => TotalReviews == 0 ? "Пока нет оценок" : $"{AverageRating:0.0} из 5";

    public string ReviewCountText =>
        TotalReviews == 0 ? "Станьте первым, кто оставит отзыв." : $"{TotalReviews} отзыв(ов) опубликовано";

    public bool HasOwnReview => _currentReviewId.HasValue;

    public bool CanDeleteReview => HasOwnReview && !IsDeletingReview;

    public string OwnReviewStateText =>
        !HasOwnReview
            ? "Поделитесь впечатлением о курсе."
            : _currentReviewApproved
                ? "Ваш отзыв опубликован и виден другим пользователям."
                : "Ваш отзыв отправлен и ждёт модерации.";

    public string ReviewButtonText =>
        IsSavingReview
            ? (HasOwnReview ? "Сохраняем..." : "Отправляем...")
            : (HasOwnReview ? "Сохранить отзыв" : "Оставить отзыв");

    public AsyncRelayCommand EnrollCommand => _enrollCommand;

    public AsyncRelayCommand SaveReviewCommand => _saveReviewCommand;

    public AsyncRelayCommand DeleteReviewCommand => _deleteReviewCommand;

    public bool ShowEmptyState => !IsLoading && string.IsNullOrWhiteSpace(ErrorMessage) && Sections.Count == 0;

    public bool ShowReviewsEmptyState => !IsReviewsLoading && string.IsNullOrWhiteSpace(ReviewsErrorMessage) && Reviews.Count == 0;

    public async Task LoadAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
        EnrollmentStatusMessage = null;
        EnrollmentErrorMessage = null;
        Sections.Clear();
        RaisePropertyChanged(nameof(ShowEmptyState));

        try
        {
            var course = await _coursesClient.GetByIdAsync(_courseId);
            ApplyCourseDetails(course);

            var sections = await _sectionsClient.GetByCourseIdAsync(_courseId);
            foreach (var section in sections.OrderBy(s => s.SectionOrder))
            {
                var sectionViewModel = new CourseSectionViewModel
                {
                    SectionId = section.SectionId,
                    SectionOrder = section.SectionOrder,
                    Title = section.Title,
                    Description = section.Description
                };

                var lessons = await _lessonsClient.GetBySectionIdAsync(section.SectionId);
                foreach (var lesson in lessons.OrderBy(l => l.LessonOrder))
                {
                    sectionViewModel.Lessons.Add(new CourseLessonViewModel
                    {
                        LessonId = lesson.LessonId,
                        LessonOrder = lesson.LessonOrder,
                        SectionTitle = section.Title,
                        Title = lesson.Title,
                        LessonType = NormalizeLessonType(lesson.LessonType),
                        IsFree = lesson.IsFree,
                        DurationMinutes = lesson.DurationMinutes,
                        Content = lesson.Content,
                        FileName = lesson.FileName,
                        FileUrl = lesson.FileUrl,
                        FileType = lesson.FileType,
                        FileSize = lesson.FileSize
                    });
                }

                Sections.Add(sectionViewModel);
            }

            await LoadEnrollmentStateAsync();
            await LoadReviewsAsync(resetActionMessages: true);
            RaisePropertyChanged(nameof(ShowEmptyState));
        }
        catch (ApiException ex)
        {
            ErrorMessage = ex.ResponseBody ?? ex.Message;
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "Не удалось загрузить детали курса. Проверь, доступен ли API.";
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

    private async Task LoadEnrollmentStateAsync()
    {
        try
        {
            var enrollments = await _enrollmentsClient.GetMyAsync();
            IsEnrolled = enrollments.Any(enrollment =>
                enrollment.CourseId == _courseId &&
                !string.Equals(enrollment.Status, "expired", StringComparison.OrdinalIgnoreCase));

            if (IsEnrolled)
            {
                EnrollmentStatusMessage = "Вы уже записаны на этот курс.";
                await LoadCourseProgressAsync();
            }
            else
            {
                ResetProgress();
            }
        }
        catch
        {
            ResetProgress();
        }
    }

    private async Task EnrollAsync()
    {
        IsEnrolling = true;
        EnrollmentStatusMessage = null;
        EnrollmentErrorMessage = null;

        try
        {
            await _enrollmentsClient.EnrollAsync(_courseId);
            IsEnrolled = true;
            EnrollmentStatusMessage = "Вы успешно записались на курс.";
            await LoadCourseProgressAsync();
        }
        catch (ApiException ex) when (
            ex.StatusCode == HttpStatusCode.BadRequest &&
            (ex.ResponseBody?.Contains("Already enrolled", StringComparison.OrdinalIgnoreCase) ?? false))
        {
            IsEnrolled = true;
            EnrollmentStatusMessage = "Вы уже записаны на этот курс.";
            await LoadCourseProgressAsync();
        }
        catch (ApiException ex)
        {
            EnrollmentErrorMessage = TryExtractApiMessage(ex.ResponseBody) ?? ex.Message;
        }
        catch (HttpRequestException)
        {
            EnrollmentErrorMessage = "Не удалось записаться на курс. Проверь, доступен ли API.";
        }
        catch (Exception ex)
        {
            EnrollmentErrorMessage = ex.Message;
        }
        finally
        {
            IsEnrolling = false;
        }
    }

    private async Task LoadCourseProgressAsync()
    {
        try
        {
            var progress = await _progressClient.GetCourseProgressAsync(_courseId);
            TotalLessons = progress.TotalLessons;
            CompletedLessons = progress.CompletedLessons;
            OverallProgress = progress.OverallProgress;
        }
        catch
        {
            ResetProgress();
        }
    }

    private async Task LoadReviewsAsync(bool resetActionMessages = false)
    {
        IsReviewsLoading = true;
        ReviewsErrorMessage = null;

        if (resetActionMessages)
        {
            ReviewStatusMessage = null;
            ReviewErrorMessage = null;
        }

        try
        {
            var ratingTask = _reviewsClient.GetCourseRatingAsync(_courseId);
            var reviewsTask = _reviewsClient.GetCourseReviewsAsync(_courseId);
            await Task.WhenAll(ratingTask, reviewsTask);

            var rating = await ratingTask;
            AverageRating = rating.AverageRating;
            TotalReviews = rating.TotalReviews;

            Reviews.Clear();
            foreach (var review in (await reviewsTask).OrderByDescending(review => review.ReviewDate))
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

            await LoadOwnReviewAsync();
            RaisePropertyChanged(nameof(ShowReviewsEmptyState));
        }
        catch (ApiException ex)
        {
            ReviewsErrorMessage = TryExtractApiMessage(ex.ResponseBody) ?? ex.Message;
        }
        catch (HttpRequestException)
        {
            ReviewsErrorMessage = "Не удалось загрузить отзывы. Проверь, доступен ли API.";
        }
        catch (Exception ex)
        {
            ReviewsErrorMessage = ex.Message;
        }
        finally
        {
            IsReviewsLoading = false;
        }
    }

    private async Task LoadOwnReviewAsync()
    {
        try
        {
            var myReview = (await _reviewsClient.GetMyAsync())
                .FirstOrDefault(review => review.CourseId == _courseId);

            ApplyOwnReview(myReview);
        }
        catch
        {
            if (!HasOwnReview)
            {
                SelectedRating = 5;
                ReviewComment = string.Empty;
            }
        }
    }

    private async Task SaveReviewAsync()
    {
        IsSavingReview = true;
        ReviewStatusMessage = null;
        ReviewErrorMessage = null;

        try
        {
            if (HasOwnReview)
            {
                await _reviewsClient.UpdateAsync(
                    _currentReviewId!.Value,
                    new UpdateReviewDto
                    {
                        Rating = SelectedRating,
                        Comment = NormalizeComment(ReviewComment)
                    });

                ReviewStatusMessage = "Отзыв обновлён. После изменения он снова проходит модерацию.";
            }
            else
            {
                await _reviewsClient.CreateAsync(
                    _courseId,
                    new CreateReviewDto
                    {
                        Rating = SelectedRating,
                        Comment = NormalizeComment(ReviewComment)
                    });

                ReviewStatusMessage = "Отзыв отправлен. После модерации он появится в общем списке.";
            }

            await LoadReviewsAsync();
        }
        catch (ApiException ex)
        {
            ReviewErrorMessage = TryExtractApiMessage(ex.ResponseBody) ?? ex.Message;
        }
        catch (HttpRequestException)
        {
            ReviewErrorMessage = "Не удалось сохранить отзыв. Проверь, доступен ли API.";
        }
        catch (Exception ex)
        {
            ReviewErrorMessage = ex.Message;
        }
        finally
        {
            IsSavingReview = false;
        }
    }

    private async Task DeleteReviewAsync()
    {
        if (!HasOwnReview)
        {
            return;
        }

        IsDeletingReview = true;
        ReviewStatusMessage = null;
        ReviewErrorMessage = null;

        try
        {
            await _reviewsClient.DeleteAsync(_currentReviewId!.Value);
            ReviewStatusMessage = "Отзыв удалён.";
            ClearOwnReview();
            await LoadReviewsAsync();
        }
        catch (ApiException ex)
        {
            ReviewErrorMessage = TryExtractApiMessage(ex.ResponseBody) ?? ex.Message;
        }
        catch (HttpRequestException)
        {
            ReviewErrorMessage = "Не удалось удалить отзыв. Проверь, доступен ли API.";
        }
        catch (Exception ex)
        {
            ReviewErrorMessage = ex.Message;
        }
        finally
        {
            IsDeletingReview = false;
        }
    }

    private void ApplyOwnReview(ReviewResponseDto? review)
    {
        if (review is null)
        {
            ClearOwnReview();
            return;
        }

        _currentReviewId = review.ReviewId;
        _currentReviewApproved = review.IsApproved;
        SelectedRating = review.Rating;
        ReviewComment = review.Comment ?? string.Empty;
        RaiseReviewStateChanged();
    }

    private void ClearOwnReview()
    {
        _currentReviewId = null;
        _currentReviewApproved = false;
        SelectedRating = 5;
        ReviewComment = string.Empty;
        RaiseReviewStateChanged();
    }

    private void RaiseReviewStateChanged()
    {
        RaisePropertyChanged(nameof(HasOwnReview));
        RaisePropertyChanged(nameof(CanDeleteReview));
        RaisePropertyChanged(nameof(OwnReviewStateText));
        RaisePropertyChanged(nameof(ReviewButtonText));
        _deleteReviewCommand.RaiseCanExecuteChanged();
        _saveReviewCommand.RaiseCanExecuteChanged();
    }

    private void ResetProgress()
    {
        TotalLessons = 0;
        CompletedLessons = 0;
        OverallProgress = 0;
    }

    private void ApplyCourseDetails(CourseResponseDto course)
    {
        Title = course.Title;
        Description = string.IsNullOrWhiteSpace(course.Description)
            ? "Описание пока не заполнено."
            : course.Description;
        Level = course.Level;
        Price = FormatPrice(course.Price);
        AuthorName = string.IsNullOrWhiteSpace(course.AuthorName)
            ? "Автор не указан"
            : course.AuthorName;
        CategoryName = string.IsNullOrWhiteSpace(course.CategoryName)
            ? "Без категории"
            : course.CategoryName;
    }

    private static string FormatPrice(decimal price) =>
        price == 0 ? "Бесплатно" : $"{price:0.##} ₽";

    private static string NormalizeLessonType(string lessonType) =>
        lessonType.ToLowerInvariant() switch
        {
            "video" => "Видео",
            "text" => "Текст",
            "quiz" => "Тест",
            "file" => "Файл",
            _ => lessonType
        };

    private static string? NormalizeComment(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string? TryExtractApiMessage(string? responseBody)
    {
        if (string.IsNullOrWhiteSpace(responseBody))
        {
            return null;
        }

        const string marker = "\"message\":\"";
        var start = responseBody.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (start < 0)
        {
            return responseBody;
        }

        start += marker.Length;
        var end = responseBody.IndexOf('"', start);
        if (end < 0)
        {
            return responseBody;
        }

        return responseBody[start..end];
    }
}
