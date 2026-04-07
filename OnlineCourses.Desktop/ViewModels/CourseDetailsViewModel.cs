using System.Collections.ObjectModel;
using System.Net.Http;
using OnlineCourses.Client.Api;
using OnlineCourses.Models.DTOs;

namespace OnlineCourses.Desktop.ViewModels;

public sealed class CourseDetailsViewModel : ViewModelBase
{
    private readonly int _courseId;
    private readonly CoursesClient _coursesClient;
    private readonly SectionsClient _sectionsClient;
    private readonly LessonsClient _lessonsClient;
    private bool _isLoading;
    private string? _errorMessage;
    private string _title;
    private string _description;
    private string _level;
    private string _price;
    private string _authorName;
    private string _categoryName;

    public CourseDetailsViewModel(
        CourseCardViewModel course,
        CoursesClient coursesClient,
        SectionsClient sectionsClient,
        LessonsClient lessonsClient)
    {
        _courseId = course.Id;
        _coursesClient = coursesClient;
        _sectionsClient = sectionsClient;
        _lessonsClient = lessonsClient;
        _title = course.Title;
        _description = course.Description;
        _level = course.Level;
        _price = FormatPrice(course.Price);
        _authorName = "Автор загружается...";
        _categoryName = "Категория загружается...";

        Sections = new ObservableCollection<CourseSectionViewModel>();
    }

    public ObservableCollection<CourseSectionViewModel> Sections { get; }

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

    public bool ShowEmptyState => !IsLoading && string.IsNullOrWhiteSpace(ErrorMessage) && Sections.Count == 0;

    public async Task LoadAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
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
                        Title = lesson.Title,
                        LessonType = NormalizeLessonType(lesson.LessonType),
                        IsFree = lesson.IsFree,
                        DurationMinutes = lesson.DurationMinutes,
                        Content = lesson.Content
                    });
                }

                Sections.Add(sectionViewModel);
            }

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
}
