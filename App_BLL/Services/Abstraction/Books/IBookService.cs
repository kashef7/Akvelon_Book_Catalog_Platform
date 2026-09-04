using App_BLL.Common.Paging;
using App_BLL.Common.Result;
using App_BLL.Dtos.BooksDtos;
using App_BLL.QueryParams.Book;

namespace App_BLL.Services.Abstraction.Books;

public interface IBookService
{
    //Get All
    Task<Result<PagedResult<BookGetDto>>> GetAllBooksAsync(BookQueryParams query, CancellationToken cancellationToken);
    //Get by id
    Task<Result<BookGetDto>> GetBookAsync(Guid id, CancellationToken cancellationToken);
    //Get by Isbn
    Task<Result<BookGetDto>> GetBookByIsbnAsync(string isbn, CancellationToken cancellationToken);
    //Create Book
    Task<Result<Guid>> AddBookAsync(BookCreateDto book, CancellationToken cancellationToken);
    //Update Book
    Task<Result> UpdateBookAsync(BookEditDto book, Guid editedBookId, CancellationToken cancellationToken);
    //Update Rating
    Task<Result> UpdateBookRatingAsync(Guid id, decimal rating, CancellationToken cancellationToken);
    //Delete Book
    Task<Result> DeleteBookAsync(Guid id, CancellationToken cancellationToken);

}