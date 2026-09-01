using System.ComponentModel.DataAnnotations;
using App_Common.Common.Book;

namespace App_BLL.Dtos.BooksDtos;

public class BookEditDto
{
    [Required]
    [MaxLength(100)]
    public string Title { get; set; }
    [Required]
    [MaxLength(1000)]
    public string Description { get; set; }
    
    [Required]
    public DateOnly DatePublished { get;  set;}
    [Required]
    [Range(0, 5)]
    public decimal Rating { get;  set; }
}