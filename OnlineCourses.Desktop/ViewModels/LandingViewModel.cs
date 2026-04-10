using System.Collections.ObjectModel;
using System.Net.Http;
using OnlineCourses.Client.Api;

namespace OnlineCourses.Desktop.ViewModels;

public sealed class LandingViewModel : ViewModelBase
{
    private readonly CoursesClient _coursesClient;
    private bool _isLoading;
    private string? _errorMessage;
    private int _coursesCount;
    private int _categoriesCount;

    public LandingViewModel(CoursesClient coursesClient)
    {
        _coursesClient = coursesClient;
        Categories = new ObservableCollection<string>();
        FeaturedCourses = new ObservableCollection<LandingCourseCardViewModel>();
    }

    public ObservableCollection<string> Categories { get; }

    public ObservableCollection<LandingCourseCardViewModel> FeaturedCourses { get; }

    public bool IsLoading
    {
        get => _isLoading;
        private set => SetProperty(ref _isLoading, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public int CoursesCount
    {
        get => _coursesCount;
        private set => SetProperty(ref _coursesCount, value);
    }

    public int CategoriesCount
    {
        get => _categoriesCount;
        private set => SetProperty(ref _categoriesCount, value);
    }

    public async Task LoadAsync()
    {
        if (IsLoading)
        {
            return;
        }

        IsLoading = true;
        ErrorMessage = null;
        Categories.Clear();
        FeaturedCourses.Clear();

        try
        {
            var coursesTask = _coursesClient.GetAllAsync(pageNumber: 1, pageSize: 6, all: false);
            var categoriesTask = _coursesClient.GetCategoriesAsync();

            await Task.WhenAll(coursesTask, categoriesTask);

            var coursePage = await coursesTask;
            var categories = (await categoriesTask).OrderBy(item => item.Name).ToList();

            CoursesCount = coursePage.TotalCount;
            CategoriesCount = categories.Count;

            foreach (var category in categories.Take(6))
            {
                if (!string.IsNullOrWhiteSpace(category.Name))
                {
                    Categories.Add(category.Name);
                }
            }

            foreach (var course in coursePage.Items.Take(4))
            {
                FeaturedCourses.Add(new LandingCourseCardViewModel
                {
                    Id = course.CourseId,
                    Title = course.Title,
                    Description = course.Description,
                    CategoryName = course.CategoryName ?? "Без категории",
                    Level = course.Level,
                    Price = course.Price,
                    AuthorName = course.AuthorName,
                    TotalStudents = course.TotalStudents
                });
            }
        }
        catch (ApiException ex)
        {
            ErrorMessage = GetFriendlyApiError(ex, "Не удалось загрузить публичную витрину курсов.");
        }
        catch (HttpRequestException)
        {
            ErrorMessage = GetFriendlyConnectionError("Не удалось загрузить публичную витрину курсов. Проверь, доступен ли API.");
        }
        catch (Exception ex)
        {
            ErrorMessage = GetFriendlyUnexpectedError(ex, "Не удалось загрузить публичную витрину курсов.");
        }
        finally
        {
            IsLoading = false;
        }
    }
}
