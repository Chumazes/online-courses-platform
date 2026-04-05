namespace OnlineCourses.Models.DTOs;

public class PaginationParams
{
    private const int MaxPageSize = 50;
    private int _pageSize = 10;
    
    public int PageNumber { get; set; } = 1;
    
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = (value > MaxPageSize) ? MaxPageSize : value;
    }
}

public class CourseFilterParams : PaginationParams
{
    public bool All { get; set; } = false;  // Для teacher/admin, показывает все курсы включая черновики
    public string? Level { get; set; }
    public int? CategoryId { get; set; }
    public string? Search { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public string? SortBy { get; set; } // title, price, rating, createdAt
    public string? SortOrder { get; set; } = "asc";
}

public class PaginatedResponse<T>
{
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public bool HasPrevious => PageNumber > 1;
    public bool HasNext => PageNumber < TotalPages;
    public List<T> Items { get; set; } = new();
}