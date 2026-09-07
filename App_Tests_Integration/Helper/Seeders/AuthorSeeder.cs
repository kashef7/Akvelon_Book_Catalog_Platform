namespace App_Tests_Integration.Helper.Seeders;

using App_DAL.Database;
using App_DAL.Entities.Authors;

public class AuthorOptions
{
    public string Name { get; set; } = "Default Test Author";
    public bool IsDeleted { get; set; } = false;
}

public class AuthorSeeder
{
    private readonly AppDbContext _db;

    public AuthorSeeder(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Author> SeedOneAsync(Action<AuthorOptions>? configure = null)
    {
        var options = new AuthorOptions();
        configure?.Invoke(options);

        var author = new Author(options.Name);

        if (options.IsDeleted)
        {
            author.DeleteAuthor();
        }

        _db.Authors.Add(author);
        await _db.SaveChangesAsync();
        return author;
    }

    public async Task<List<Author>> SeedManyAsync(int count, Action<AuthorOptions, int>? configure = null)
    {
        var authors = new List<Author>();

        for (int i = 0; i < count; i++)
        {
            var options = new AuthorOptions
            {
                Name = $"Test Author {i + 1}"
            };

            configure?.Invoke(options, i);

            var author = new Author(options.Name);

            if (options.IsDeleted)
            {
                author.DeleteAuthor();
            }

            _db.Authors.Add(author);
            authors.Add(author);
        }

        await _db.SaveChangesAsync();
        return authors;
    }
}
