using App_DAL.Entities.Books;
using App_DAL.Entities.Users;

namespace App_DAL.Entities.Loans;

public class Loan
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    
    public DateTime LoanedAt { get; init; }
    public DateTime? ReturnedAt { get; private set; }
    public DateTime DueAt { get; init; }
    
    public Guid BookId { get; init; }
    public Book Book { get; init; }
    public Guid UserId { get; init; }
    public User User { get; init; }
    
    private Loan() {}
    
    public Loan(DateTime dueAt, Book book, User user)
    {
        LoanedAt = DateTime.UtcNow;
        DueAt =  dueAt;
        Book = book;
        User = user;
        BookId = book.Id;
        UserId = user.Id;
    }

    public void ReturnBook()
    {
        ReturnedAt = DateTime.UtcNow;
    }
}