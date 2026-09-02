namespace App_Tests_Integration.Helper.Seeders;

using App_DAL.Database;
using App_DAL.Entities.Authors;
using App_DAL.Entities.Books;

public class BookOptions
{
    public string Isbn { get; set; } = $"978{Random.Shared.Next(100000000, 999999999)}";
    public string Title { get; set; } = "Default Test Book";
    public string Description { get; set; } = "Default test description for integration tests.";
    public DateOnly DatePublished { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
    public decimal Rating { get; set; } = 4.25m;
    public Author? Author { get; set; } = null;
    public bool IsDeleted { get; set; } = false;
}

public class BookSeeder
{
    private readonly AppDbContext _db;

    public BookSeeder(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Book> SeedOneAsync(Action<BookOptions>? configure = null)
    {
        var options = new BookOptions();
        configure?.Invoke(options);

        var author = options.Author;
        if (author == null)
        {
            author = new Author("Default Author");
            _db.Authors.Add(author);
        }

        var book = new Book(
            options.Isbn,
            options.Title,
            options.Description,
            author,
            options.DatePublished,
            options.Rating
        );

        if (options.IsDeleted)
        {
            book.DeleteBook();
        }

        _db.Books.Add(book);
        await _db.SaveChangesAsync();
        return book;
    }

    public async Task<List<Book>> SeedManyAsync(int count, Action<BookOptions, int>? configure = null)
    {
        var books = new List<Book>();

        var defaultAuthor = new Author("Shared Test Author");
        _db.Authors.Add(defaultAuthor);

        for (int i = 0; i < count; i++)
        {
            var options = new BookOptions
            {
                Isbn = $"978{i:D10}",
                Title = $"Test Book {i + 1}",
                Author = defaultAuthor
            };
            
            configure?.Invoke(options, i);

            var author = options.Author ?? defaultAuthor;

            var book = new Book(
                options.Isbn,
                options.Title,
                options.Description,
                author,
                options.DatePublished,
                options.Rating
            );

            if (options.IsDeleted)
            {
                book.DeleteBook();
            }

            _db.Books.Add(book);
            books.Add(book);
        }

        await _db.SaveChangesAsync();
        return books;
    }
}