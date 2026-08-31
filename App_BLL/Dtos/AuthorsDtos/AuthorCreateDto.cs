using System.ComponentModel.DataAnnotations;

namespace App_BLL.Dtos.AuthorsDtos;

public class AuthorCreateDto
{
    [Required]
    [MaxLength(64)]
    public string Name { get; set; }
}