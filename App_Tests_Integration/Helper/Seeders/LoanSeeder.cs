namespace App_Tests_Integration.Helper.Seeders;

using App_DAL.Database;
using App_DAL.Entities.Books;
using App_DAL.Entities.Loans;
using App_DAL.Entities.Users;

public class LoanOptions
{
    public DateTime DueAt { get; set; } = DateTime.UtcNow.AddDays(14);
    public Book? Book { get; set; } = null;
    public User? User { get; set; } = null;
    public bool MarkAsReturned { get; set; } = false;
}

public class LoanSeeder
{
    private readonly AppDbContext _db;

    public LoanSeeder(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Loan> SeedOneAsync(Action<LoanOptions>? configure = null)
    {
        var options = new LoanOptions();
        configure?.Invoke(options);

        var book = options.Book ?? await new BookSeeder(_db).SeedOneAsync();
        var user = options.User ?? await new UserSeeder(_db).SeedOneAsync();

        var loan = new Loan(options.DueAt, book, user);

        if (options.MarkAsReturned)
        {
            loan.ReturnBook();
        }

        _db.Loans.Add(loan);
        await _db.SaveChangesAsync();
        return loan;
    }

    public async Task<List<Loan>> SeedManyAsync(int count, Action<LoanOptions, int>? configure = null)
    {
        var loans = new List<Loan>();
        var defaultUser = await new UserSeeder(_db).SeedOneAsync();

        for (int i = 0; i < count; i++)
        {
            var options = new LoanOptions
            {
                User = defaultUser
            };

            configure?.Invoke(options, i);

            var book = options.Book ?? await new BookSeeder(_db).SeedOneAsync();
            var user = options.User ?? defaultUser;

            var loan = new Loan(options.DueAt, book, user);

            if (options.MarkAsReturned)
            {
                loan.ReturnBook();
            }

            _db.Loans.Add(loan);
            loans.Add(loan);
        }

        await _db.SaveChangesAsync();
        return loans;
    }
}
