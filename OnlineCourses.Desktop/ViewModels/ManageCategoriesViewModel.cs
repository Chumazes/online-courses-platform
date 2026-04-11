using System.Collections.ObjectModel;
using System.Net.Http;
using OnlineCourses.Client.Api;
using OnlineCourses.Desktop.Infrastructure;
using OnlineCourses.Models.DTOs;

namespace OnlineCourses.Desktop.ViewModels;

public sealed class ManageCategoriesViewModel : ViewModelBase
{
    private readonly CoursesClient _coursesClient;
    private readonly AsyncRelayCommand _saveCommand;
    private ManageCategoryItemViewModel? _selectedCategory;
    private bool _isLoading;
    private bool _isSaving;
    private string _name = string.Empty;
    private string _description = string.Empty;
    private string? _statusMessage;
    private string? _errorMessage;

    public ManageCategoriesViewModel(CoursesClient coursesClient)
    {
        _coursesClient = coursesClient;
        _saveCommand = new AsyncRelayCommand(SaveAsync, CanSave);
        Categories = new ObservableCollection<ManageCategoryItemViewModel>();
    }

    public ObservableCollection<ManageCategoryItemViewModel> Categories { get; }

    public ManageCategoryItemViewModel? SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            if (SetProperty(ref _selectedCategory, value))
            {
                ApplySelection(value);
                RaisePropertyChanged(nameof(IsExistingCategory));
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

    public bool IsExistingCategory => SelectedCategory is not null;

    public bool ShowEmptyState =>
        !IsLoading &&
        string.IsNullOrWhiteSpace(ErrorMessage) &&
        Categories.Count == 0;

    public string EditorTitle => IsExistingCategory ? "Редактирование категории" : "Новая категория";

    public string SaveButtonText => IsSaving ? "Сохраняем..." : IsExistingCategory ? "Сохранить категорию" : "Создать категорию";

    public string Name
    {
        get => _name;
        set
        {
            if (SetProperty(ref _name, value))
            {
                _saveCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string Description
    {
        get => _description;
        set => SetProperty(ref _description, value);
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
        var selectedCategoryId = SelectedCategory?.CategoryId;
        IsLoading = true;
        ErrorMessage = null;
        StatusMessage = null;
        Categories.Clear();

        try
        {
            var categories = await _coursesClient.GetCategoriesAsync();
            foreach (var category in categories.OrderBy(item => item.Name))
            {
                Categories.Add(Map(category));
            }

            if (selectedCategoryId is not null)
            {
                SelectedCategory = Categories.FirstOrDefault(item => item.CategoryId == selectedCategoryId.Value);
            }
        }
        catch (ApiException ex)
        {
            ErrorMessage = GetFriendlyApiError(ex, "Не удалось загрузить категории.");
        }
        catch (HttpRequestException)
        {
            ErrorMessage = GetFriendlyConnectionError("Не удалось загрузить категории. Проверь, доступен ли API.");
        }
        catch (Exception ex)
        {
            ErrorMessage = GetFriendlyUnexpectedError(ex, "Не удалось загрузить категории.");
        }
        finally
        {
            IsLoading = false;
        }
    }

    public void StartCreating()
    {
        SelectedCategory = null;
        Name = string.Empty;
        Description = string.Empty;
        StatusMessage = null;
        ErrorMessage = null;
        RaisePropertyChanged(nameof(IsExistingCategory));
        RaisePropertyChanged(nameof(EditorTitle));
        RaisePropertyChanged(nameof(SaveButtonText));
        _saveCommand.RaiseCanExecuteChanged();
    }

    public async Task DeleteSelectedAsync()
    {
        if (SelectedCategory is null)
        {
            return;
        }

        try
        {
            ErrorMessage = null;
            StatusMessage = null;
            await _coursesClient.DeleteCategoryAsync(SelectedCategory.CategoryId);
            StatusMessage = "Категория удалена.";
            StartCreating();
            await LoadAsync();
        }
        catch (ApiException ex)
        {
            ErrorMessage = GetFriendlyApiError(ex, "Не удалось удалить категорию.");
        }
        catch (HttpRequestException)
        {
            ErrorMessage = GetFriendlyConnectionError("Не удалось удалить категорию. Проверь, доступен ли API.");
        }
        catch (Exception ex)
        {
            ErrorMessage = GetFriendlyUnexpectedError(ex, "Не удалось удалить категорию.");
        }
    }

    private void ApplySelection(ManageCategoryItemViewModel? category)
    {
        if (category is null)
        {
            return;
        }

        Name = category.Name;
        Description = category.Description ?? string.Empty;
        StatusMessage = null;
        ErrorMessage = null;
    }

    private async Task SaveAsync()
    {
        if (!CanSave())
        {
            ErrorMessage = "Название категории обязательно.";
            return;
        }

        IsSaving = true;
        ErrorMessage = null;
        StatusMessage = null;

        try
        {
            if (SelectedCategory is null)
            {
                var created = await _coursesClient.CreateCategoryAsync(new CreateCourseCategoryDto
                {
                    Name = Name.Trim(),
                    Description = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim(),
                    ParentCategoryId = null
                });

                await LoadAsync();
                SelectedCategory = Categories.FirstOrDefault(item => item.CategoryId == created.CategoryId);
                StatusMessage = "Категория создана.";
            }
            else
            {
                var categoryId = SelectedCategory.CategoryId;
                await _coursesClient.UpdateCategoryAsync(categoryId, new UpdateCourseCategoryDto
                {
                    Name = Name.Trim(),
                    Description = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim(),
                    ParentCategoryId = null
                });

                await LoadAsync();
                SelectedCategory = Categories.FirstOrDefault(item => item.CategoryId == categoryId);
                StatusMessage = "Категория обновлена.";
            }
        }
        catch (ApiException ex)
        {
            ErrorMessage = GetFriendlyApiError(ex, "Не удалось сохранить категорию.");
        }
        catch (HttpRequestException)
        {
            ErrorMessage = GetFriendlyConnectionError("Не удалось сохранить категорию. Проверь, доступен ли API.");
        }
        catch (Exception ex)
        {
            ErrorMessage = GetFriendlyUnexpectedError(ex, "Не удалось сохранить категорию.");
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
               !string.IsNullOrWhiteSpace(Name);
    }

    private static ManageCategoryItemViewModel Map(CourseCategoryDto category) =>
        new()
        {
            CategoryId = category.CategoryId,
            Name = category.Name,
            Description = category.Description,
            ParentCategoryId = category.ParentCategoryId
        };
}
