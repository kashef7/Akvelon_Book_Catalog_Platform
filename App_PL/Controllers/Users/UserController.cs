using App_BLL.Common.Result;
using App_BLL.Dtos.UsersDtos;
using App_BLL.QueryParams.User;
using App_BLL.Services.Abstraction.Users;
using App_PL.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace App_PL.Controllers.Users;

[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;

    public UserController(IUserService userService)
    {
        _userService = userService;
    }
    
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] UserQueryParams query, CancellationToken cancellationToken)
    {
        var result = await _userService.GetAllUsersAsync(query, cancellationToken);
        return result.IsSuccess ? Ok(result.Data) : HandleFailure(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _userService.GetUserAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(result.Data) : HandleFailure(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create(UserCreateDto dto, CancellationToken cancellationToken)
    {
        var result = await _userService.AddUserAsync(dto, cancellationToken);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Data }, new { id = result.Data })
            : HandleFailure(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UserEditDto dto, CancellationToken cancellationToken)
    {
        var result = await _userService.UpdateUserAsync(dto, id, cancellationToken);
        return result.IsSuccess ? NoContent() : HandleFailure(result);
    }
    
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _userService.DeleteUserAsync(id, cancellationToken);
        return result.IsSuccess ? NoContent() : HandleFailure(result);
    }
    
    private IActionResult HandleFailure(Result result) =>
        Problem(detail: result.Message, statusCode: result.Error!.Value.ToHttpStatusCode());
}
