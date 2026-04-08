using System.Collections.ObjectModel;
using System.Globalization;
using System.Net.Http;
using OnlineCourses.Client.Api;
using OnlineCourses.Desktop.Infrastructure;
using OnlineCourses.Models.DTOs;

namespace OnlineCourses.Desktop.ViewModels;

public sealed class ManageCoursesViewModel : ViewModelBase
{
    private readonly CoursesClient _coursesClient;
    private readonly AsyncRelayCommand _saveCommand;
    private ManageCourseItemViewModel? _selectedCourse;
    private bool _isLoading;
    private bool _isSaving;
    private string _title = string.Empty;
    private string _description = string.Empty;
    private string _level = "beginner";
    private string _price = "0";
    private string _status = "draft";
    private string? _coverImageUrl;
    private string? _statusMessage;
    private string? _errorMessage;

    public ManageCoursesViewModel(CoursesClient coursesClient)
    {
        _coursesClient = coursesClient;
        _saveCommand = new AsyncRelayCommand(SaveAsync, CanSave);

        Courses = new ObservableCollection<ManageCourseItemViewModel>();
        Levels = new[] { "beginner", "intermediate", "advanced" };
        Statuses = new[] { "draft", "published", "archived" };
    }

    public ObservableCollection<ManageCourseItemViewModel> Courses { get; }
    public IReadOnlyList<string> Levels { get; }
    public IReadOnlyList<string> Statuses { get; }

    public ManageCourseItemViewModel? SelectedCourse
    {
        get => _selectedCourse;
        set
        {
            if (SetProperty(ref _selectedCourse, value))
            {
                ApplySelection(value);
                RaisePropertyChanged(nameof(IsExistingCourse));
                RaisePropertyChanged(nameof(EditorTitle));
                _saveCommand.RaiseCanExecuteChanged();
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
                _saveCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public bool IsSaving
    {
        get => _isSaving;
        private set
        {
            if (SetProperty(ref _isSaving, value))
            {
                RaisePropertyChanged(nameof(SaveButtonText));
                _saveCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public bool IsExistingCourse => SelectedCourse is not null;

    public bool ShowEmptyState =>
        !IsLoading &&
        string.IsNullOrWhiteSpace(ErrorMessage) &&
        Courses.Count == 0;

    public string EditorTitle => IsExistingCourse ? "Редактирование курса" : "Новый курс";

    public string SaveButtonText => IsSaving ? "Сохраняем..." : IsExistingCourse ? "Сохранить изменения" : "Создать курс";

    public string Title
    {
        get => _title;
        set
        {
            if (SetProperty(ref _title, value))
            {
                _saveCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string Description
    {
        get => _description;
        set
        {
            if (SetProperty(ref _description, value))
            {
                _saveCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string Level
    {
        get => _level;
        set => SetProperty(ref _level, value);
    }

    public string Price
    {
        get => _price;
        set
        {
            if (SetProperty(ref _price, value))
            {
                _saveCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }

    public string? CoverImageUrl
    {
        get => _coverImageUrl;
        set => SetProperty(ref _coverImageUrl, value);
    }

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

    public AsyncRelayCommand SaveCommand => _saveCommand;

    public async Task LoadAsync()
    {
        var selectedCourseId = SelectedCourse?.CourseId;
        IsLoading = true;
        ErrorMessage = null;
        StatusMessage = null;
        Courses.Clear();

        try
        {
            var courses = await _coursesClient.GetMyAsync();
            foreach (var course in courses.OrderByDescending(item => item.CreatedAt))
            {
                Courses.Add(MapCourse(course));
            }

            if (selectedCourseId is not null)
            {
                SelectedCourse = Courses.FirstOrDefault(item => item.CourseId == selectedCourseId.Value);
            }
        }
        catch (ApiException ex)
        {
            ErrorMessage = ex.ResponseBody ?? ex.Message;
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "Не удалось загрузить курсы автора. Проверь, доступен ли API.";
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

    public void StartCreating()
    {
        SelectedCourse = null;
        Title = string.Empty;
        Description = string.Empty;
        Level = Levels[0];
        Price = "0";
        Status = Statuses[0];
        CoverImageUrl = string.Empty;
        StatusMessage = null;
        ErrorMessage = null;
        RaisePropertyChanged(nameof(IsExistingCourse));
        RaisePropertyChanged(nameof(EditorTitle));
        RaisePropertyChanged(nameof(SaveButtonText));
        _saveCommand.RaiseCanExecuteChanged();
    }

    public async Task DeleteSelectedAsync()
    {
        if (SelectedCourse is null)
        {
            return;
        }

        try
        {
            ErrorMessage = null;
            StatusMessage = null;
            var deletedCourseId = SelectedCourse.CourseId;

            await _coursesClient.DeleteAsync(deletedCourseId);
            StatusMessage = "Курс удалён.";
            StartCreating();
            await LoadAsync();
        }
        catch (ApiException ex)
        {
            ErrorMessage = ex.ResponseBody ?? ex.Message;
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "Не удалось удалить курс. Проверь, доступен ли API.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    private void ApplySelection(ManageCourseItemViewModel? course)
    {
        if (course is null)
        {
            return;
        }

        Title = course.Title;
        Description = course.Description;
        Level = course.Level;
        Price = course.Price.ToString("0.##", CultureInfo.InvariantCulture);
        Status = course.Status;
        CoverImageUrl = course.CoverImageUrl;
        StatusMessage = null;
        ErrorMessage = null;
    }

    private async Task SaveAsync()
    {
        if (!TryParsePrice(out var parsedPrice))
        {
            ErrorMessage = "Цена должна быть числом.";
            return;
        }

        IsSaving = true;
        StatusMessage = null;
        ErrorMessage = null;

        try
        {
            if (SelectedCourse is null)
            {
                var created = await _coursesClient.CreateAsync(new CreateCourseDto
                {
                    Title = Title.Trim(),
                    Description = Description.Trim(),
                    Price = parsedPrice,
                    Level = Level,
                    CoverImageUrl = string.IsNullOrWhiteSpace(CoverImageUrl) ? null : CoverImageUrl.Trim()
                });

                await LoadAsync();
                SelectedCourse = Courses.FirstOrDefault(item => item.CourseId == created.CourseId);
                StatusMessage = "Курс создан.";
            }
            else
            {
                var courseId = SelectedCourse.CourseId;
                await _coursesClient.UpdateAsync(SelectedCourse.CourseId, new UpdateCourseDto
                {
                    Title = Title.Trim(),
                    Description = Description.Trim(),
                    Price = parsedPrice,
                    Level = Level,
                    Status = Status,
                    CoverImageUrl = string.IsNullOrWhiteSpace(CoverImageUrl) ? null : CoverImageUrl.Trim()
                });

                await LoadAsync();
                SelectedCourse = Courses.FirstOrDefault(item => item.CourseId == courseId);
                StatusMessage = "Курс обновлён.";
            }
        }
        catch (ApiException ex)
        {
            ErrorMessage = ex.ResponseBody ?? ex.Message;
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "Не удалось сохранить курс. Проверь, доступен ли API.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsSaving = false;
        }
    }

    private bool CanSave()
    {
        return !IsLoading &&
               !IsSaving &&
               !string.IsNullOrWhiteSpace(Title) &&
               !string.IsNullOrWhiteSpace(Description) &&
               TryParsePrice(out _);
    }

    private bool TryParsePrice(out decimal parsedPrice)
    {
        var raw = Price.Replace(',', '.');
        return decimal.TryParse(raw, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out parsedPrice);
    }

    private static ManageCourseItemViewModel MapCourse(CourseResponseDto course) =>
        new()
        {
            CourseId = course.CourseId,
            Title = course.Title,
            Description = course.Description,
            Level = course.Level,
            Price = course.Price,
            Status = string.IsNullOrWhiteSpace(course.Status) ? "draft" : course.Status,
            CoverImageUrl = course.CoverImageUrl,
            CreatedAt = course.CreatedAt,
            TotalStudents = course.TotalStudents
        };
}
