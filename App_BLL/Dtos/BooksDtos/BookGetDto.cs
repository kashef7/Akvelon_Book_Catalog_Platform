using System.ComponentModel.DataAnnotations;
using App_Common.Common.Book;

namespace App_BLL.Dtos.BooksDtos;

public class BookGetDto
{

    public Guid Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public string AuthorName { get;  set; }
    public DateOnly DatePublished { get;  set;}
    public decimal Rating { get;  set; }
    public BookStatus Status { get;  set; }
}