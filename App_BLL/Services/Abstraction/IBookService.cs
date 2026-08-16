using App_BLL.Common.Result;
using App_BLL.Dtos.BooksDtos;

namespace App_BLL.Services.Abstraction;

public interface IBookService
{
    //Get All
    Task<Result<IReadOnlyList<BookGetDto>>> GetAllBooksAsync();
    //Get by id
    Task<Result<BookGetDto>> GetBookAsync(Guid id);
    //Create Book
    Task<Result<Guid>> AddBookAsync(BookCreateDto book);
    //Update Book
    Task<Result> UpdateBookAsync(BookEditDto book, Guid editedBookId);
    //Update Status
    Task<Result> UpdateBookStatusAsync(Guid id, BookStatusDto status);
    //Update Rating
    Task<Result> UpdateBookRatingAsync(Guid id, decimal rating);
    //Delete Book
    Task<Result> DeleteBookAsync(Guid id);

}