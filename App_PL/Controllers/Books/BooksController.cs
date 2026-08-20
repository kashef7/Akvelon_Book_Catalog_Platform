using App_BLL.Common.Result;
using App_BLL.Dtos.BooksDtos;
using App_BLL.Services.Abstraction.Books;
using Microsoft.AspNetCore.Mvc;

namespace App_PL.Controllers;

// TODO: add [EnumDataType(typeof(BookStatus))] validation to BookStatusDto
// TODO: enforce/round rating to 2 decimal places before persisting

[ApiController]
[Route("api/[controller]")]
public class BooksController : ControllerBase
{
    private readonly IBookService _bookService;

    public BooksController(IBookService bookService)
    {
        _bookService = bookService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _bookService.GetAllBooksAsync();
        return result.IsSuccess ? Ok(result.Data) : HandleFailure(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _bookService.GetBookAsync(id);
        return result.IsSuccess ? Ok(result.Data) : HandleFailure(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create(BookCreateDto dto)
    {
        var result = await _bookService.AddBookAsync(dto);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Data }, new { id = result.Data })
            : HandleFailure(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, BookEditDto dto)
    {
        var result = await _bookService.UpdateBookAsync(dto, id);
        return result.IsSuccess ? NoContent() : HandleFailure(result);
    }
    
    [HttpPatch("status/{id:guid}")]
    public async Task<IActionResult> UpdateStatus(Guid id, BookStatusDto status)
    {
        var result = await _bookService.UpdateBookStatusAsync(id, status);
        return result.IsSuccess ? NoContent() : HandleFailure(result);
    }
    
    [HttpPatch("rating/{id:guid}")]
    public async Task<IActionResult> UpdateRating(Guid id, BookRatingDto rating)
    {
        var result = await _bookService.UpdateBookRatingAsync(id, rating.Rating);
        return result.IsSuccess ? NoContent() : HandleFailure(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _bookService.DeleteBookAsync(id);
        return result.IsSuccess ? NoContent() : HandleFailure(result);
    }
    
    private IActionResult HandleFailure(Result result) =>
        Problem(detail: result.Message, statusCode: result.StatusCode);
}