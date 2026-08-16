// App_BLL/Dtos/BooksDtos/BookRatingUpdateDto.cs
using System.ComponentModel.DataAnnotations;

namespace App_BLL.Dtos.BooksDtos;

public class BookRatingDto
{
    [Required]
    [Range(0, 5)]
    public decimal Rating { get; set; }
}