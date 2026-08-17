using System.ComponentModel.DataAnnotations;
using App_DAL.Entities;

namespace App_BLL.Dtos.BooksDtos;

public class BookStatusDto
{
    [Required]  
    public BookStatus Status{ get; set; }
}