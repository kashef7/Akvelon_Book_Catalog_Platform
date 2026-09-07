using App_Common.Common.User;
using App_DAL.Entities.Users;

namespace App_DAL.Repos.Abstraction.Users;

public interface IUserRepo
{
    Task<(IReadOnlyList<User> items, int TotalCount)> GetAllUsersAsync(UserQuery userQuery, CancellationToken cancellationToken);
    Task<User?> GetUserByIdAsync(Guid id, CancellationToken cancellationToken);
    Task AddUserAsync(User user);
    Task SaveChangesAsync();
}
