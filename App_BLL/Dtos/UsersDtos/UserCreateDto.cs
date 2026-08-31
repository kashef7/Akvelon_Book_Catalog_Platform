using System.ComponentModel.DataAnnotations;

namespace App_BLL.Dtos.UsersDtos;

public class UserCreateDto
{
    [Required]
    [MaxLength(64)]
    public string Name { get; set; }
}
