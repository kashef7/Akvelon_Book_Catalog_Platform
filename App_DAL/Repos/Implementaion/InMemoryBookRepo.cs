using System.Collections.Concurrent;
using App_DAL.Entities;
using App_DAL.Repos.Abstraction;

namespace App_DAL.Repos.Implementaion;

public class InMemoryBookRepo : IBookRepo
{
    private readonly ConcurrentDictionary<Guid, Book> _books = new();

    public Task<List<Book>> GetAllBooksAsync()
    {
        var result = _books.Values.Where(b => !b.IsDeleted).ToList();
        return Task.FromResult(result);
    }

    public Task<Book?> GetBookByIdAsync(Guid id)
    {
        _books.TryGetValue(id, out var book);
        return Task.FromResult(book is { IsDeleted: false } ? book : null);
    }

    public Task AddBookAsync(Book book)
    {
        _books.TryAdd(book.Id, book);
        return Task.CompletedTask;
    }

    public Task DeleteBookAsync(Guid id)
    {
        if (_books.TryGetValue(id, out var book))
            book.DeleteBook();
        return Task.CompletedTask;
    }
}