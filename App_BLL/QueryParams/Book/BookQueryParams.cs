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
    

    [EnumDataType(typeof(BookStatus))]
    public BookStatus? Status { get; init;}
    
    [Range(0,5)]
    public decimal? Rating { get; init;}
}