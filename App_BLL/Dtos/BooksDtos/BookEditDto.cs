using System.ComponentModel.DataAnnotations;
using App_DAL.Entities;

namespace App_BLL.Dtos.BooksDtos;

public class BookEditDto
{
    [Required]
    [MaxLength(100)]
    public string Title { get; set; }
    [Required]
    [MaxLength(300)]
    public string Description { get; set; }
    [Required]
    [MaxLength(70)]
    public string AuthorName { get;  set; }
    [Required]
    public DateOnly DatePublished { get;  set;}
    [Required]
    [Range(0, 5)]
    public decimal Rating { get;  set; }
    [Required]
    public BookStatus Status { get;  set; }
}