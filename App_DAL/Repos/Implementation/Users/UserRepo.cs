using App_Common.Common.User;
using App_DAL.Database;
using App_DAL.Entities.Users;
using App_DAL.Filters.Users;
using App_DAL.Repos.Abstraction.Users;
using Microsoft.EntityFrameworkCore;

namespace App_DAL.Repos.Implementation.Users;

public class UserRepo : IUserRepo
{
    private readonly AppDbContext _dbContext;
    
    public UserRepo(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    
    public async Task<(IReadOnlyList<User> items, int TotalCount)> GetAllUsersAsync(UserQuery userQuery)
    {
        var query = _dbContext.Users.AsNoTracking().AsQueryable().Where(u => !u.IsDeleted).ApplyQueryFilters(userQuery);
        
        var totalCount = await query.CountAsync();
        
        IReadOnlyList<User> result = await query.OrderBy(x => x.Id).Skip((userQuery.PageNumber - 1) * userQuery.PageSize).Take(userQuery.PageSize).ToListAsync();
        return (result, totalCount);
    }

    public async Task<User?> GetUserByIdAsync(Guid id)
    {
        var query = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);
        return query;
    }

    public async Task AddUserAsync(User user)
    {
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();
    }

    public async Task SaveChangesAsync()
    {
        await _dbContext.SaveChangesAsync();
    }
}
