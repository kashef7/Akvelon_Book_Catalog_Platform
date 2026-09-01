namespace App_BLL.QueryParams.Loan;

public class LoanQueryParams
{
    private const int MaxPageSize = 50;
    private int _pageSize = 10;

    public int PageNumber { get; set; } = 1;
    public int PageSize
    {
        get => _pageSize;
        init => _pageSize = value > MaxPageSize ? MaxPageSize : value;
    }

    public Guid? BookId { get; init; }
    public Guid? UserId { get; init; }
    public bool? IsReturned { get; init; }
    public DateTime? DueBefore { get; init; }
    public DateTime? DueAfter { get; init; }
}