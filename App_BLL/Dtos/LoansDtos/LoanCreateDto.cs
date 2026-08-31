using System.ComponentModel.DataAnnotations;

namespace App_BLL.Dtos.LoansDtos;

public class LoanCreateDto
{
    [Required]
    public DateTime DueAt  { get; set; }
    [Required]
    public Guid BookId { get; set; }
    [Required]
    public Guid UserId { get; set; }
}