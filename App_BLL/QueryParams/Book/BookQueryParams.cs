using System.ComponentModel.DataAnnotations;
using App_Common.Common.Book;

namespace App_BLL.QueryParams.Book;

public class BookQueryParams
{
    private const int MaxPageSize = 50;
    private int _pageSize = 10;

    public int PageNumber { get; set; } = 1;
    public int PageSize
    {
        get => _pageSize;
        init => _pageSize = value > MaxPageSize ? MaxPageSize : value;
    }
    
    [MaxLength(100)]
    public string? Title { get; init;}
    
    [Range(0,5)]
    public decimal? Rating { get; init;}
    
    [MaxLength(64)]
    public string? AuthorName { get; init;}
    
    public DateOnly? StartDatePublished { get; init;}
    public DateOnly? EndDatePublished { get; init;}
}