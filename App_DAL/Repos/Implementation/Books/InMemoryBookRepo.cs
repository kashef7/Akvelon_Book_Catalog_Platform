// using System.Collections.Concurrent;
// using App_Common.Common.Book;
// using App_DAL.Entities.Books;
// using App_DAL.Filters.Books;
// using App_DAL.Repos.Abstraction.Books;
//
// namespace App_DAL.Repos.Implementation.Books;
//
// public class InMemoryBookRepo : IBookRepo
// {
//     private readonly ConcurrentDictionary<Guid, Book> _books = new();
//
//     public Task<(IReadOnlyList<Book>,int)> GetAllBooksAsync(BookQuery bookQuery)
//     {
//         var query = _books.Values.AsQueryable().Where(b => !b.IsDeleted).ApplyQueryFilters(bookQuery);
//         
//         int totalCount = query.Count();
//         IReadOnlyList<Book> result = query.Skip((bookQuery.PageNumber - 1) * bookQuery.PageSize).Take(bookQuery.PageSize).ToList();
//         return Task.FromResult<(IReadOnlyList<Book>,int)>((result, totalCount));
//     }
//
//     public Task<Book?> GetBookByIdAsync(Guid id)
//     {
//         _books.TryGetValue(id, out var book);
//         return Task.FromResult(book is { IsDeleted: false } ? book : null);
//     }
//
//     public Task AddBookAsync(Book book)
//     {
//         _books.TryAdd(book.Id, book);
//         return Task.CompletedTask;
//     }
//
//     public async Task UpdateBookAsync(Book editBook, Book editedBook)
//     {
//         throw new NotImplementedException();
//     }
//
//     public Task DeleteBookAsync(Guid id)
//     {
//         if (_books.TryGetValue(id, out var book))
//             book.DeleteBook();
//         return Task.CompletedTask;
//     }
// }