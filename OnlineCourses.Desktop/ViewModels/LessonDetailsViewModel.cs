using System.Net;
using System.Net.Http;
using OnlineCourses.Client.Api;
using OnlineCourses.Desktop.Infrastructure;
using OnlineCourses.Models.DTOs;

namespace OnlineCourses.Desktop.ViewModels;

public sealed class LessonDetailsViewModel : ViewModelBase
{
    private readonly int _lessonId;
    private readonly int _watchTime;
    private readonly ProgressClient _progressClient;
    private readonly AsyncRelayCommand _completeCommand;
    private bool _isCompleted;
    private bool _isCompleting;
    private bool _canTrackProgress;
    private string? _progressStatusMessage;
    private string? _progressErrorMessage;

    public LessonDetailsViewModel(CourseLessonViewModel lesson, ProgressClient progressClient)
    {
        _lessonId = lesson.LessonId;
        _watchTime = (lesson.DurationMinutes ?? 0) * 60;
        _progressClient = progressClient;
        _completeCommand = new AsyncRelayCommand(CompleteAsync, () => CanTrackProgress && !IsCompleted && !IsCompleting);

        Title = lesson.Title;
        SectionTitle = string.IsNullOrWhiteSpace(lesson.SectionTitle)
            ? "Раздел не указан"
            : lesson.SectionTitle;
        MetaText = lesson.MetaText;
        AccessText = lesson.AccessText;
        Content = string.IsNullOrWhiteSpace(lesson.Content)
            ? "Содержимое урока пока не заполнено."
            : lesson.Content;
        CanTrackProgress = true;
    }

    public string Title { get; }
    public string SectionTitle { get; }
    public string MetaText { get; }
    public string AccessText { get; }
    public string Content { get; }

    public bool IsCompleted
    {
        get => _isCompleted;
        private set
        {
            if (SetProperty(ref _isCompleted, value))
            {
                RaisePropertyChanged(nameof(CompletionStateText));
                RaisePropertyChanged(nameof(CompleteButtonText));
                _completeCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public bool IsCompleting
    {
        get => _isCompleting;
        private set
        {
            if (SetProperty(ref _isCompleting, value))
            {
                RaisePropertyChanged(nameof(CompleteButtonText));
                _completeCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public bool CanTrackProgress
    {
        get => _canTrackProgress;
        private set
        {
            if (SetProperty(ref _canTrackProgress, value))
            {
                RaisePropertyChanged(nameof(CompletionStateText));
                _completeCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string CompletionStateText =>
        !CanTrackProgress
            ? "Прогресс недоступен"
            : IsCompleted
                ? "Урок завершён"
                : "Урок ещё не завершён";

    public string CompleteButtonText =>
        IsCompleted ? "Урок завершён" : IsCompleting ? "Сохраняем..." : "Отметить как завершённый";

    public string? ProgressStatusMessage
    {
        get => _progressStatusMessage;
        private set => SetProperty(ref _progressStatusMessage, value);
    }

    public string? ProgressErrorMessage
    {
        get => _progressErrorMessage;
        private set => SetProperty(ref _progressErrorMessage, value);
    }

    public AsyncRelayCommand CompleteCommand => _completeCommand;

    public async Task LoadProgressAsync()
    {
        ProgressStatusMessage = null;
        ProgressErrorMessage = null;

        try
        {
            var progress = await _progressClient.GetLessonProgressAsync(_lessonId);
            CanTrackProgress = true;
            IsCompleted = progress.IsCompleted;

            if (IsCompleted)
            {
                ProgressStatusMessage = "Этот урок уже отмечен как завершённый.";
            }
        }
        catch (ApiException ex) when (
            ex.StatusCode == HttpStatusCode.BadRequest &&
            (ex.ResponseBody?.Contains("not enrolled", StringComparison.OrdinalIgnoreCase) ?? false))
        {
            CanTrackProgress = false;
            ProgressErrorMessage = "Чтобы отмечать прогресс, сначала запишись на курс.";
        }
        catch (ApiException ex)
        {
            CanTrackProgress = false;
            ProgressErrorMessage = ex.ResponseBody ?? ex.Message;
        }
        catch (HttpRequestException)
        {
            CanTrackProgress = false;
            ProgressErrorMessage = "Не удалось загрузить прогресс урока. Проверь, доступен ли API.";
        }
        catch (Exception ex)
        {
            CanTrackProgress = false;
            ProgressErrorMessage = ex.Message;
        }
    }

    private async Task CompleteAsync()
    {
        IsCompleting = true;
        ProgressStatusMessage = null;
        ProgressErrorMessage = null;

        try
        {
            await _progressClient.UpdateProgressAsync(new UpdateProgressDto
            {
                LessonId = _lessonId,
                IsCompleted = true,
                WatchTime = _watchTime
            });

            IsCompleted = true;
            ProgressStatusMessage = "Урок отмечен как завершённый.";
        }
        catch (ApiException ex)
        {
            ProgressErrorMessage = ex.ResponseBody ?? ex.Message;
        }
        catch (HttpRequestException)
        {
            ProgressErrorMessage = "Не удалось сохранить прогресс. Проверь, доступен ли API.";
        }
        catch (Exception ex)
        {
            ProgressErrorMessage = ex.Message;
        }
        finally
        {
            IsCompleting = false;
        }
    }
}
