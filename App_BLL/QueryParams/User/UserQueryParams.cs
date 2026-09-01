using System.ComponentModel.DataAnnotations;

namespace App_BLL.QueryParams.User;

public class UserQueryParams
{
    private const int MaxPageSize = 50;
    private int _pageSize = 10;

    public int PageNumber { get; set; } = 1;
    public int PageSize
    {
        get => _pageSize;
        init => _pageSize = value > MaxPageSize ? MaxPageSize : value;
    }
    
    [MaxLength(30)]
    public string? Name { get; init;}
}
