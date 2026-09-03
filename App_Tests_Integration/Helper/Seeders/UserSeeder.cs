namespace App_Tests_Integration.Helper.Seeders;

using App_DAL.Database;
using App_DAL.Entities.Users;

public class UserOptions
{
    public string Name { get; set; } = "Default Test User";
    public bool IsDeleted { get; set; } = false;
}

public class UserSeeder
{
    private readonly AppDbContext _db;

    public UserSeeder(AppDbContext db)
    {
        _db = db;
    }

    public async Task<User> SeedOneAsync(Action<UserOptions>? configure = null)
    {
        var options = new UserOptions();
        configure?.Invoke(options);

        var user = new User(options.Name);

        if (options.IsDeleted)
        {
            user.DeleteUser();
        }

        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return user;
    }

    public async Task<List<User>> SeedManyAsync(int count, Action<UserOptions, int>? configure = null)
    {
        var users = new List<User>();

        for (int i = 0; i < count; i++)
        {
            var options = new UserOptions
            {
                Name = $"Test User {i + 1}"
            };

            configure?.Invoke(options, i);

            var user = new User(options.Name);

            if (options.IsDeleted)
            {
                user.DeleteUser();
            }

            _db.Users.Add(user);
            users.Add(user);
        }

        await _db.SaveChangesAsync();
        return users;
    }
}
