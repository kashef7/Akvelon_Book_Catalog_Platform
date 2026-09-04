using App_BLL.Common.Result;
using App_BLL.Dtos.BooksDtos;
using App_BLL.QueryParams.Book;
using App_BLL.Services.Abstraction.Books;
using App_PL.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace App_PL.Controllers;


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
    public async Task<IActionResult> GetAll([FromQuery] BookQueryParams query, CancellationToken cancellationToken)
    {
        var result = await _bookService.GetAllBooksAsync(query, cancellationToken);
        return result.IsSuccess ? Ok(result.Data) : HandleFailure(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _bookService.GetBookAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(result.Data) : HandleFailure(result);
    }
    
    [HttpGet("{isbn}")]
    public async Task<IActionResult> GetByIsbn(string isbn, CancellationToken cancellationToken)
    {
        var result = await _bookService.GetBookByIsbnAsync(isbn, cancellationToken);
        return result.IsSuccess ? Ok(result.Data) : HandleFailure(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create(BookCreateDto dto, CancellationToken cancellationToken)
    {
        var result = await _bookService.AddBookAsync(dto, cancellationToken);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Data }, new { id = result.Data })
            : HandleFailure(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, BookEditDto dto, CancellationToken cancellationToken)
    {
        var result = await _bookService.UpdateBookAsync(dto, id, cancellationToken);
        return result.IsSuccess ? NoContent() : HandleFailure(result);
    }
    
    [HttpPatch("rating/{id:guid}")]
    public async Task<IActionResult> UpdateRating(Guid id, BookRatingDto rating, CancellationToken cancellationToken)
    {
        var result = await _bookService.UpdateBookRatingAsync(id, rating.Rating, cancellationToken);
        return result.IsSuccess ? NoContent() : HandleFailure(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _bookService.DeleteBookAsync(id, cancellationToken);
        return result.IsSuccess ? NoContent() : HandleFailure(result);
    }
    
    private IActionResult HandleFailure(Result result) =>
        Problem(detail: result.Message, statusCode: result.Error!.Value.ToHttpStatusCode());
}