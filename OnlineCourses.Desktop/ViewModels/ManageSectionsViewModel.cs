using System.Collections.ObjectModel;
using System.Net.Http;
using System.Text.Json;
using OnlineCourses.Client.Api;
using OnlineCourses.Desktop.Infrastructure;
using OnlineCourses.Models.DTOs;

namespace OnlineCourses.Desktop.ViewModels;

public sealed class ManageSectionsViewModel : ViewModelBase
{
    private readonly SectionsClient _sectionsClient;
    private readonly AsyncRelayCommand _saveCommand;
    private readonly int _courseId;
    private ManageSectionItemViewModel? _selectedSection;
    private bool _isLoading;
    private bool _isSaving;
    private string _title = string.Empty;
    private string _description = string.Empty;
    private string _sectionOrder = "1";
    private string? _statusMessage;
    private string? _errorMessage;

    public ManageSectionsViewModel(int courseId, string courseTitle, SectionsClient sectionsClient)
    {
        _courseId = courseId;
        CourseTitle = string.IsNullOrWhiteSpace(courseTitle) ? "Без названия" : courseTitle;
        _sectionsClient = sectionsClient;
        _saveCommand = new AsyncRelayCommand(SaveAsync, CanSave);
        Sections = new ObservableCollection<ManageSectionItemViewModel>();
    }

    public ObservableCollection<ManageSectionItemViewModel> Sections { get; }

    public string CourseTitle { get; }

    public ManageSectionItemViewModel? SelectedSection
    {
        get => _selectedSection;
        set
        {
            if (SetProperty(ref _selectedSection, value))
            {
                ApplySelection(value);
                RaisePropertyChanged(nameof(IsExistingSection));
                RaisePropertyChanged(nameof(EditorTitle));
                RaisePropertyChanged(nameof(SaveButtonText));
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

    public bool IsExistingSection => SelectedSection is not null;

    public bool ShowEmptyState =>
        !IsLoading &&
        string.IsNullOrWhiteSpace(ErrorMessage) &&
        Sections.Count == 0;

    public string EditorTitle => IsExistingSection ? "Редактирование секции" : "Новая секция";

    public string SaveButtonText => IsSaving ? "Сохраняем..." : IsExistingSection ? "Сохранить изменения" : "Создать секцию";

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

    public string SectionOrder
    {
        get => _sectionOrder;
        set
        {
            if (SetProperty(ref _sectionOrder, value))
            {
                _saveCommand.RaiseCanExecuteChanged();
            }
        }
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
        var selectedSectionId = SelectedSection?.SectionId;
        IsLoading = true;
        ErrorMessage = null;
        StatusMessage = null;
        Sections.Clear();

        try
        {
            var sections = await _sectionsClient.GetByCourseIdAsync(_courseId);
            foreach (var section in sections.OrderBy(item => item.SectionOrder))
            {
                Sections.Add(MapSection(section));
            }

            if (selectedSectionId is not null)
            {
                SelectedSection = Sections.FirstOrDefault(item => item.SectionId == selectedSectionId.Value);
            }
        }
        catch (ApiException ex)
        {
            ErrorMessage = ExtractApiErrorMessage(ex);
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "Не удалось загрузить секции курса. Проверь, доступен ли API.";
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
        SelectedSection = null;
        Title = string.Empty;
        Description = string.Empty;
        SectionOrder = GetNextSectionOrder().ToString();
        StatusMessage = null;
        ErrorMessage = null;
        RaisePropertyChanged(nameof(IsExistingSection));
        RaisePropertyChanged(nameof(EditorTitle));
        RaisePropertyChanged(nameof(SaveButtonText));
        _saveCommand.RaiseCanExecuteChanged();
    }

    public async Task DeleteSelectedAsync()
    {
        if (SelectedSection is null)
        {
            return;
        }

        try
        {
            ErrorMessage = null;
            StatusMessage = null;
            var deletedSectionId = SelectedSection.SectionId;

            await _sectionsClient.DeleteAsync(_courseId, deletedSectionId);
            StatusMessage = "Секция удалена.";
            StartCreating();
            await LoadAsync();
        }
        catch (ApiException ex)
        {
            ErrorMessage = ExtractApiErrorMessage(ex);
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "Не удалось удалить секцию. Проверь, доступен ли API.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    private void ApplySelection(ManageSectionItemViewModel? section)
    {
        if (section is null)
        {
            return;
        }

        Title = section.Title;
        Description = section.Description;
        SectionOrder = section.SectionOrder.ToString();
        StatusMessage = null;
        ErrorMessage = null;
    }

    private async Task SaveAsync()
    {
        if (!TryParseSectionOrder(out var parsedOrder))
        {
            ErrorMessage = "Порядок секции должен быть целым числом.";
            return;
        }

        if (IsSectionOrderTaken(parsedOrder))
        {
            ErrorMessage = "Секция с таким порядковым номером уже существует. Выбери другой номер.";
            return;
        }

        IsSaving = true;
        StatusMessage = null;
        ErrorMessage = null;

        try
        {
            if (SelectedSection is null)
            {
                var created = await _sectionsClient.CreateAsync(_courseId, new CreateSectionDto
                {
                    Title = Title.Trim(),
                    Description = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim(),
                    SectionOrder = parsedOrder
                });

                await LoadAsync();
                SelectedSection = Sections.FirstOrDefault(item => item.SectionId == created.SectionId);
                StatusMessage = "Секция создана.";
            }
            else
            {
                var sectionId = SelectedSection.SectionId;
                await _sectionsClient.UpdateAsync(_courseId, sectionId, new UpdateSectionDto
                {
                    Title = Title.Trim(),
                    Description = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim(),
                    SectionOrder = parsedOrder
                });

                await LoadAsync();
                SelectedSection = Sections.FirstOrDefault(item => item.SectionId == sectionId);
                StatusMessage = "Секция обновлена.";
            }
        }
        catch (ApiException ex)
        {
            ErrorMessage = ExtractApiErrorMessage(ex);
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "Не удалось сохранить секцию. Проверь, доступен ли API.";
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
               TryParseSectionOrder(out _);
    }

    private bool TryParseSectionOrder(out int parsedOrder)
    {
        return int.TryParse(SectionOrder, out parsedOrder) && parsedOrder > 0;
    }

    private int GetNextSectionOrder()
    {
        return Sections.Count == 0 ? 1 : Sections.Max(item => item.SectionOrder) + 1;
    }

    private bool IsSectionOrderTaken(int parsedOrder)
    {
        return Sections.Any(item =>
            item.SectionOrder == parsedOrder &&
            item.SectionId != SelectedSection?.SectionId);
    }

    private static ManageSectionItemViewModel MapSection(SectionResponseDto section) =>
        new()
        {
            SectionId = section.SectionId,
            CourseId = section.CourseId,
            CourseTitle = section.CourseTitle,
            Title = section.Title,
            Description = section.Description ?? string.Empty,
            SectionOrder = section.SectionOrder,
            LessonsCount = section.LessonsCount,
            CreatedAt = section.CreatedAt
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
}
