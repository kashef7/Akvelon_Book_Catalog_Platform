using App_BLL.Common.Result;
using App_BLL.Dtos.AuthorsDtos;
using App_BLL.QueryParams.Author;
using App_BLL.Services.Abstraction.Authors;
using App_PL.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace App_PL.Controllers.Authors;

[ApiController]
[Route("api/[controller]")]
public class AuthorController : ControllerBase
{
    private readonly IAuthorService _authorService;
    public AuthorController(IAuthorService authorService)
    {
        _authorService = authorService;
    }
    
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] AuthorQueryParams query, CancellationToken cancellationToken)
    {
        var result = await _authorService.GetAllAuthorsAsync(query, cancellationToken);
        return result.IsSuccess ? Ok(result.Data) : HandleFailure(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _authorService.GetAuthorAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(result.Data) : HandleFailure(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create(AuthorCreateDto dto, CancellationToken cancellationToken)
    {
        var result = await _authorService.AddAuthorAsync(dto, cancellationToken);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Data }, new { id = result.Data })
            : HandleFailure(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, AuthorEditDto dto, CancellationToken cancellationToken)
    {
        var result = await _authorService.UpdateAuthorAsync(dto, id, cancellationToken);
        return result.IsSuccess ? NoContent() : HandleFailure(result);
    }
    
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _authorService.DeleteAuthorAsync(id, cancellationToken);
        return result.IsSuccess ? NoContent() : HandleFailure(result);
    }
    
    private IActionResult HandleFailure(Result result) =>
        Problem(detail: result.Message, statusCode: result.Error!.Value.ToHttpStatusCode());
    
}