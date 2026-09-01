using App_Common.Common.Book;
using App_DAL.Database;
using App_DAL.Entities.Books;
using App_DAL.Filters.Books;
using App_DAL.Repos.Abstraction.Books;
using Microsoft.EntityFrameworkCore;

namespace App_DAL.Repos.Implementation.Books;

public class BookRepo : IBookRepo
{
    private readonly AppDbContext _dbContext;
    public BookRepo(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    public async Task<(IReadOnlyList<Book> items, int totalCount)> GetAllBooksAsync(BookQuery bookQuery)
    {
        var query = _dbContext.Books.AsNoTracking().AsQueryable().Where(b => b.IsDeleted == false).ApplyQueryFilters(bookQuery).Include(b => b.Author);
        
        int totalCount = await query.CountAsync();

        var paginatedQuery = query.OrderBy(x => x.Id).Skip((bookQuery.PageNumber - 1) * bookQuery.PageSize)
            .Take(bookQuery.PageSize);
        
        var sql = paginatedQuery.ToQueryString();
        
        IReadOnlyList<Book> result = await paginatedQuery.ToListAsync();
        return (result, totalCount);
    }

    public async Task<Book?> GetBookByIdAsync(Guid id)
    {
        var book = await _dbContext.Books.Include(b => b.Author).FirstOrDefaultAsync(b => b.Id == id && b.IsDeleted == false);
        return book;
    }
    
    public async Task<Book?> GetBookByIsbnAsync(string isbn)
    {
        var book = await _dbContext.Books.Include(b => b.Author).FirstOrDefaultAsync(b => b.Isbn == isbn && b.IsDeleted == false);
        return book;
    }

    public async Task AddBookAsync(Book book)
    {
        _dbContext.Books.Add(book);
        await _dbContext.SaveChangesAsync();
    }

    public async Task SaveChangesAsync()
    {
        await _dbContext.SaveChangesAsync();
    }

    public async Task<bool> HasActiveBookByAuthorAsync(Guid authorId)
    {
        return await _dbContext.Books.AnyAsync(b => b.AuthorId == authorId &&  b.IsDeleted == false);
    }
}