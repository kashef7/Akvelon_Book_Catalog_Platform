using App_Common.Common.Book;
using App_DAL.Entities.Books;

namespace App_DAL.Repos.Abstraction.Books;

public interface IBookRepo
{
    Task<(IReadOnlyList<Book>,int)> GetAllBooksAsync(BookQuery bookQuery);
    Task<Book?> GetBookByIdAsync(Guid id);
    Task AddBookAsync(Book book);
    Task DeleteBookAsync(Guid id);
}