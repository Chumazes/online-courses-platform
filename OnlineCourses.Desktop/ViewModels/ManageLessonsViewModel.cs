using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using OnlineCourses.Client.Api;
using OnlineCourses.Desktop.Infrastructure;
using OnlineCourses.Models.DTOs;

namespace OnlineCourses.Desktop.ViewModels;

public sealed class ManageLessonsViewModel : ViewModelBase
{
    private readonly LessonsClient _lessonsClient;
    private readonly FilesClient _filesClient;
    private readonly AsyncRelayCommand _saveCommand;
    private readonly int _sectionId;
    private ManageLessonItemViewModel? _selectedLesson;
    private bool _isLoading;
    private bool _isSaving;
    private bool _isUploadingFile;
    private string _title = string.Empty;
    private string _content = string.Empty;
    private string _lessonType = "text";
    private string _videoUrl = string.Empty;
    private string _durationMinutes = string.Empty;
    private string _lessonOrder = "1";
    private bool _isFree;
    private string? _uploadedFileName;
    private string? _uploadedFileUrl;
    private string? _uploadedFileDisplayUrl;
    private string? _uploadedFileSizeText;
    private string? _statusMessage;
    private string? _errorMessage;

    public ManageLessonsViewModel(
        int sectionId,
        string sectionTitle,
        LessonsClient lessonsClient,
        FilesClient filesClient)
    {
        _sectionId = sectionId;
        SectionTitle = string.IsNullOrWhiteSpace(sectionTitle) ? "Без названия" : sectionTitle;
        _lessonsClient = lessonsClient;
        _filesClient = filesClient;
        _saveCommand = new AsyncRelayCommand(SaveAsync, CanSave);
        Lessons = new ObservableCollection<ManageLessonItemViewModel>();
        LessonTypes = new[] { "text", "video", "quiz" };
    }

    public ObservableCollection<ManageLessonItemViewModel> Lessons { get; }
    public IReadOnlyList<string> LessonTypes { get; }

    public string SectionTitle { get; }

    public ManageLessonItemViewModel? SelectedLesson
    {
        get => _selectedLesson;
        set
        {
            if (SetProperty(ref _selectedLesson, value))
            {
                ApplySelection(value);
                RaisePropertyChanged(nameof(IsExistingLesson));
                RaisePropertyChanged(nameof(EditorTitle));
                RaisePropertyChanged(nameof(SaveButtonText));
                RaisePropertyChanged(nameof(CanUploadFile));
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
                RaisePropertyChanged(nameof(CanUploadFile));
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
                RaisePropertyChanged(nameof(CanUploadFile));
            }
        }
    }

    public bool IsUploadingFile
    {
        get => _isUploadingFile;
        private set
        {
            if (SetProperty(ref _isUploadingFile, value))
            {
                RaisePropertyChanged(nameof(UploadButtonText));
                RaisePropertyChanged(nameof(CanUploadFile));
            }
        }
    }

    public bool IsExistingLesson => SelectedLesson is not null;

    public bool CanUploadFile => IsExistingLesson && !IsUploadingFile && !IsSaving;

    public bool ShowEmptyState =>
        !IsLoading &&
        string.IsNullOrWhiteSpace(ErrorMessage) &&
        Lessons.Count == 0;

    public string EditorTitle => IsExistingLesson ? "Редактирование урока" : "Новый урок";

    public string SaveButtonText => IsSaving ? "Сохраняем..." : IsExistingLesson ? "Сохранить изменения" : "Создать урок";

    public string UploadButtonText => IsUploadingFile ? "Загружаем файл..." : "Загрузить файл";

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

    public string Content
    {
        get => _content;
        set
        {
            if (SetProperty(ref _content, value))
            {
                _saveCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string LessonType
    {
        get => _lessonType;
        set => SetProperty(ref _lessonType, value);
    }

    public string VideoUrl
    {
        get => _videoUrl;
        set => SetProperty(ref _videoUrl, value);
    }

    public string DurationMinutes
    {
        get => _durationMinutes;
        set
        {
            if (SetProperty(ref _durationMinutes, value))
            {
                _saveCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string LessonOrder
    {
        get => _lessonOrder;
        set
        {
            if (SetProperty(ref _lessonOrder, value))
            {
                _saveCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public bool IsFree
    {
        get => _isFree;
        set => SetProperty(ref _isFree, value);
    }

    public string? UploadedFileName
    {
        get => _uploadedFileName;
        private set
        {
            if (SetProperty(ref _uploadedFileName, value))
            {
                RaisePropertyChanged(nameof(HasUploadedFile));
            }
        }
    }

    public string? UploadedFileUrl
    {
        get => _uploadedFileUrl;
        private set => SetProperty(ref _uploadedFileUrl, value);
    }

    public string? UploadedFileDisplayUrl
    {
        get => _uploadedFileDisplayUrl;
        private set => SetProperty(ref _uploadedFileDisplayUrl, value);
    }

    public string? UploadedFileSizeText
    {
        get => _uploadedFileSizeText;
        private set => SetProperty(ref _uploadedFileSizeText, value);
    }

    public bool HasUploadedFile => !string.IsNullOrWhiteSpace(UploadedFileName);

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
        var selectedLessonId = SelectedLesson?.LessonId;
        IsLoading = true;
        ErrorMessage = null;
        StatusMessage = null;
        Lessons.Clear();

        try
        {
            var lessons = await _lessonsClient.GetBySectionIdAsync(_sectionId);
            foreach (var lesson in lessons.OrderBy(item => item.LessonOrder))
            {
                Lessons.Add(MapLesson(lesson));
            }

            if (selectedLessonId is not null)
            {
                SelectedLesson = Lessons.FirstOrDefault(item => item.LessonId == selectedLessonId.Value);
            }
        }
        catch (ApiException ex)
        {
            ErrorMessage = ExtractApiErrorMessage(ex);
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "Не удалось загрузить уроки секции. Проверь, доступен ли API.";
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
        SelectedLesson = null;
        Title = string.Empty;
        Content = string.Empty;
        LessonType = LessonTypes[0];
        VideoUrl = string.Empty;
        DurationMinutes = string.Empty;
        LessonOrder = GetNextLessonOrder().ToString();
        IsFree = false;
        ApplyUploadedFile(null, null, null);
        StatusMessage = null;
        ErrorMessage = null;
        RaisePropertyChanged(nameof(IsExistingLesson));
        RaisePropertyChanged(nameof(EditorTitle));
        RaisePropertyChanged(nameof(SaveButtonText));
        _saveCommand.RaiseCanExecuteChanged();
    }

    public async Task DeleteSelectedAsync()
    {
        if (SelectedLesson is null)
        {
            return;
        }

        try
        {
            ErrorMessage = null;
            StatusMessage = null;

            await _lessonsClient.DeleteAsync(_sectionId, SelectedLesson.LessonId);
            StatusMessage = "Урок удален.";
            StartCreating();
            await LoadAsync();
        }
        catch (ApiException ex)
        {
            ErrorMessage = ExtractApiErrorMessage(ex);
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "Не удалось удалить урок. Проверь, доступен ли API.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    private void ApplySelection(ManageLessonItemViewModel? lesson)
    {
        if (lesson is null)
        {
            return;
        }

        Title = lesson.Title;
        Content = lesson.Content;
        LessonType = lesson.LessonType;
        VideoUrl = lesson.VideoUrl ?? string.Empty;
        DurationMinutes = lesson.DurationMinutes?.ToString() ?? string.Empty;
        LessonOrder = lesson.LessonOrder.ToString();
        IsFree = lesson.IsFree;
        ApplyUploadedFile(lesson.FileName, lesson.FileUrl, lesson.FileSize);
        StatusMessage = null;
        ErrorMessage = null;
    }

    public async Task UploadFileAsync(string filePath)
    {
        if (SelectedLesson is null)
        {
            ErrorMessage = "Сначала сохрани урок, а потом загружай файл.";
            return;
        }

        IsUploadingFile = true;
        StatusMessage = null;
        ErrorMessage = null;

        try
        {
            var uploadTitle = string.IsNullOrWhiteSpace(Title)
                ? Path.GetFileNameWithoutExtension(filePath)
                : Title.Trim();

            var lessonId = SelectedLesson.LessonId;
            await _filesClient.UploadLessonFileAsync(lessonId, filePath, uploadTitle);
            await LoadAsync();
            SelectedLesson = Lessons.FirstOrDefault(item => item.LessonId == lessonId);
            StatusMessage = "Файл урока загружен.";
        }
        catch (ApiException ex)
        {
            ErrorMessage = ExtractApiErrorMessage(ex);
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "Не удалось загрузить файл урока. Проверь, доступен ли API.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsUploadingFile = false;
        }
    }

    private async Task SaveAsync()
    {
        if (!TryParseLessonOrder(out var parsedOrder))
        {
            ErrorMessage = "Порядок урока должен быть целым числом.";
            return;
        }

        if (IsLessonOrderTaken(parsedOrder))
        {
            ErrorMessage = "Урок с таким порядковым номером уже существует. Выбери другой номер.";
            return;
        }

        if (!TryParseDuration(out var parsedDuration))
        {
            ErrorMessage = "Длительность должна быть целым числом или пустым значением.";
            return;
        }

        IsSaving = true;
        StatusMessage = null;
        ErrorMessage = null;

        try
        {
            if (SelectedLesson is null)
            {
                var created = await _lessonsClient.CreateAsync(_sectionId, new CreateLessonDto
                {
                    Title = Title.Trim(),
                    Content = string.IsNullOrWhiteSpace(Content) ? null : Content.Trim(),
                    LessonType = LessonType,
                    VideoUrl = string.IsNullOrWhiteSpace(VideoUrl) ? null : VideoUrl.Trim(),
                    DurationMinutes = parsedDuration,
                    LessonOrder = parsedOrder,
                    IsFree = IsFree
                });

                await LoadAsync();
                SelectedLesson = Lessons.FirstOrDefault(item => item.LessonId == created.LessonId);
                StatusMessage = "Урок создан.";
            }
            else
            {
                var lessonId = SelectedLesson.LessonId;
                await _lessonsClient.UpdateAsync(_sectionId, lessonId, new UpdateLessonDto
                {
                    Title = Title.Trim(),
                    Content = string.IsNullOrWhiteSpace(Content) ? null : Content.Trim(),
                    LessonType = LessonType,
                    VideoUrl = string.IsNullOrWhiteSpace(VideoUrl) ? null : VideoUrl.Trim(),
                    DurationMinutes = parsedDuration,
                    LessonOrder = parsedOrder,
                    IsFree = IsFree
                });

                await LoadAsync();
                SelectedLesson = Lessons.FirstOrDefault(item => item.LessonId == lessonId);
                StatusMessage = "Урок обновлен.";
            }
        }
        catch (ApiException ex)
        {
            ErrorMessage = ExtractApiErrorMessage(ex);
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "Не удалось сохранить урок. Проверь, доступен ли API.";
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
               TryParseLessonOrder(out _) &&
               TryParseDuration(out _);
    }

    private bool TryParseLessonOrder(out int parsedOrder)
    {
        return int.TryParse(LessonOrder, out parsedOrder) && parsedOrder > 0;
    }

    private bool TryParseDuration(out int? parsedDuration)
    {
        if (string.IsNullOrWhiteSpace(DurationMinutes))
        {
            parsedDuration = null;
            return true;
        }

        var success = int.TryParse(DurationMinutes, out var duration) && duration > 0;
        parsedDuration = success ? duration : null;
        return success;
    }

    private int GetNextLessonOrder()
    {
        return Lessons.Count == 0 ? 1 : Lessons.Max(item => item.LessonOrder) + 1;
    }

    private void ApplyUploadedFile(string? fileName, string? fileUrl, long? fileSize)
    {
        if (string.IsNullOrWhiteSpace(fileUrl))
        {
            UploadedFileName = null;
            UploadedFileUrl = null;
            UploadedFileDisplayUrl = null;
            UploadedFileSizeText = null;
            return;
        }

        UploadedFileName = string.IsNullOrWhiteSpace(fileName) ? "Файл урока" : fileName;
        UploadedFileUrl = fileUrl;
        UploadedFileDisplayUrl = _filesClient.BuildDownloadUrl(fileUrl);
        UploadedFileSizeText = fileSize.HasValue ? FormatFileSize(fileSize.Value) : null;
    }

    private bool IsLessonOrderTaken(int parsedOrder)
    {
        return Lessons.Any(item =>
            item.LessonOrder == parsedOrder &&
            item.LessonId != SelectedLesson?.LessonId);
    }

    private static ManageLessonItemViewModel MapLesson(LessonResponseDto lesson) =>
        new()
        {
            LessonId = lesson.LessonId,
            SectionId = lesson.SectionId,
            Title = lesson.Title,
            Content = lesson.Content ?? string.Empty,
            LessonType = string.IsNullOrWhiteSpace(lesson.LessonType) ? "text" : lesson.LessonType,
            VideoUrl = lesson.VideoUrl,
            FileName = lesson.FileName,
            FileUrl = lesson.FileUrl,
            FileType = lesson.FileType,
            FileSize = lesson.FileSize,
            DurationMinutes = lesson.DurationMinutes,
            LessonOrder = lesson.LessonOrder,
            IsFree = lesson.IsFree,
            CreatedAt = lesson.CreatedAt
        };

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

    private static string FormatFileSize(long fileSize)
    {
        if (fileSize <= 0)
        {
            return "0 Б";
        }

        const double kb = 1024d;
        const double mb = kb * 1024d;

        if (fileSize >= mb)
        {
            return $"{fileSize / mb:0.##} МБ";
        }

        if (fileSize >= kb)
        {
            return $"{fileSize / kb:0.##} КБ";
        }

        return fileSize.ToString("0", CultureInfo.InvariantCulture) + " Б";
    }
}
