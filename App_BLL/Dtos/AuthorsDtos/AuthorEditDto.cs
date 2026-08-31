using System.ComponentModel.DataAnnotations;

namespace App_BLL.Dtos.AuthorsDtos;

public class AuthorEditDto
{
    [Required]
    [MaxLength(64)]
    public string Name { get; set; }
}