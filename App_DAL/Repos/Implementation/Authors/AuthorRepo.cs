using App_Common.Common.Author;
using App_DAL.Database;
using App_DAL.Entities.Authors;
using App_DAL.Filters.Authors;
using App_DAL.Repos.Abstraction.Authors;
using Microsoft.EntityFrameworkCore;

namespace App_DAL.Repos.Implementation.Authors;

public class AuthorRepo : IAuthorRepo
{
    private readonly AppDbContext _dbContext;
    
    public AuthorRepo(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    
    public async Task<(IReadOnlyList<Author> items, int TotalCount)> GetAllAuthorsAsync(AuthorQuery authorQuery)
    {
        var query = _dbContext.Authors.AsNoTracking().AsQueryable().Where(b => !b.IsDeleted).ApplyQueryFilters(authorQuery);
        
        var totalCount = await query.CountAsync();
        
        IReadOnlyList<Author> result = await query.OrderBy(x => x.Id).Skip((authorQuery.PageNumber - 1) * authorQuery.PageSize).Take(authorQuery.PageSize).ToListAsync();
        return (result, totalCount);
    }

    public async Task<Author?> GetAuthorByIdAsync(Guid id)
    {
        var query = await _dbContext.Authors.FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);
        return  query;
    }

    public async Task AddAuthorAsync(Author author)
    {
        _dbContext.Authors.Add(author);
        await _dbContext.SaveChangesAsync();
    }

    public async Task SaveChangesAsync()
    {
        await _dbContext.SaveChangesAsync();
    }
}