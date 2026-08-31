namespace App_Common.Common.Loan;

public class LoanQuery
{
    public int PageNumber { get; init;}
    public int PageSize { get; init;}
    
    public Guid? BookId { get; init; }
    public Guid? UserId { get; init; }
    public bool? IsReturned { get; init; }
    public DateTime? DueBefore { get; init; }
    public DateTime? DueAfter { get; init; }
}