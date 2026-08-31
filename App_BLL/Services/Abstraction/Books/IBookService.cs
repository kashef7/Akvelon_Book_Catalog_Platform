using App_BLL.Common.Paging;
using App_BLL.Common.Result;
using App_BLL.Dtos.BooksDtos;
using App_BLL.QueryParams.Book;

namespace App_BLL.Services.Abstraction.Books;

public interface IBookService
{
    //Get All
    Task<Result<PagedResult<BookGetDto>>> GetAllBooksAsync(BookQueryParams query);
    //Get by id
    Task<Result<BookGetDto>> GetBookAsync(Guid id);
    //Get by Isbn
    Task<Result<BookGetDto>> GetBookByIsbnAsync(string isbn);
    //Create Book
    Task<Result<Guid>> AddBookAsync(BookCreateDto book);
    //Update Book
    Task<Result> UpdateBookAsync(BookEditDto book, Guid editedBookId);
    //Update Rating
    Task<Result> UpdateBookRatingAsync(Guid id, decimal rating);
    //Delete Book
    Task<Result> DeleteBookAsync(Guid id);

}