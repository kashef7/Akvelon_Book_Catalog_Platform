using System.ComponentModel.DataAnnotations;
using App_Common.Common.Book;

namespace App_BLL.Dtos.BooksDtos;


public class BookStatusDto
{
    [Required]  
    [EnumDataType(typeof(BookStatus))]
    public BookStatus Status{ get; set; }
}