using App_Common.Common.User;
using App_DAL.Entities.Users;

namespace App_DAL.Filters.Users;

public static class UserFilters
{
    public static IQueryable<User> ApplyQueryFilters(this IQueryable<User> users, UserQuery userQuery)
    {
        if (!string.IsNullOrEmpty(userQuery.Name))
        {
            users = users.Where(a => a.Name.Contains(userQuery.Name));
        }
        return users;
    }
}
