namespace OnlineCourses.Desktop.ViewModels;

public sealed class ManageCategoryItemViewModel
{
    public int CategoryId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public int? ParentCategoryId { get; init; }

    public string DescriptionText =>
        string.IsNullOrWhiteSpace(Description)
            ? "Без описания"
            : Description!;

    public string MetaText =>
        ParentCategoryId is > 0
            ? $"Родительская категория: {ParentCategoryId}"
            : "Самостоятельная категория";
}
