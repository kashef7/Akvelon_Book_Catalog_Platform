using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices;
using App_Common.Common.Book;

namespace App_BLL.Dtos.BooksDtos;


public class BookCreateDto 
{
    [Required]
    [MaxLength(100)]
    public string Title { get; set; }
    [Required]
    [MaxLength(1000)]
    public string Description { get; set; }
    
    [Required]
    [Length(13,13)]
    public string Isbn { get; set; }
    
    [Required]
    public Guid AuthorId { get; set; }
    
    [Required]
    public DateOnly DatePublished { get;  set;}
    [Required]
    [Range(0, 5)]
    public decimal Rating { get;  set; }
}