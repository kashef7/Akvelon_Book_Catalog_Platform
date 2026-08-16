using System.ComponentModel.DataAnnotations;
using App_DAL.Entities;

namespace App_BLL.Dtos.BooksDtos;

public class BookStatusDto
{
    [Required]  
    BookStatus status{ get; set; }
}