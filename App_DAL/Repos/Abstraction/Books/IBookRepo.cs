using App_Common.Common.Book;
using App_DAL.Entities.Books;

namespace App_DAL.Repos.Abstraction.Books;

public interface IBookRepo
{
    Task<(IReadOnlyList<Book> items ,int totalCount)> GetAllBooksAsync(BookQuery bookQuery, CancellationToken cancellationToken);
    Task<Book?> GetBookByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<Book?> GetBookByIsbnAsync(string isbn, CancellationToken cancellationToken);
    Task AddBookAsync(Book book);
    Task SaveChangesAsync();
    Task<bool> HasActiveBookByAuthorAsync(Guid authorId, CancellationToken cancellationToken);
}