namespace App_BLL.Dtos.LoansDtos;

public class LoanGetDto
{
    public Guid LoanId { get; set; }
    public DateTime LoanedAt { get; set; }
    public DateTime DueAt { get; set; }
    public DateTime? ReturnedAt { get; set; }
    
    public string BookTitle { get; set; }
    public Guid BookId { get; set; }
    public string UserName { get; set; }
    public Guid UserId { get; set; }
    
}