using App_DAL.Entities;

namespace App_DAL.Repos.Abstraction;

public interface IBookRepo
{
    Task<IReadOnlyList<Book>> GetAllBooksAsync();
    Task<Book?> GetBookByIdAsync(Guid id);
    Task AddBookAsync(Book book);
    Task DeleteBookAsync(Guid id);
}