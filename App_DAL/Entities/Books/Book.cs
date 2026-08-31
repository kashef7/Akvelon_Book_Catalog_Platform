using App_DAL.Entities.Authors;

namespace App_DAL.Entities.Books;

public class Book
{
    public Guid Id { get; init; } = Guid.CreateVersion7(); 
    public string Isbn { get; init; }
    public string Title { get; private set; }
    public string Description { get; private set; }
    public DateOnly DatePublished { get; private set; }
    public decimal Rating { get; private set; }

    public Guid AuthorId { get; init; }
    public Author Author { get; init; }
    public bool IsDeleted { get; private set; }
    
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; private set; }
    
    public DateTime? DeletedAt { get; private set; }
    
    
    private Book() {}
    
    public Book(string isbn,string title, string description, Author author, DateOnly datePublished, decimal rating)
    {
        Isbn = isbn;
        Title = title;
        Description = description;
        AuthorId = author.Id;
        Author = author;
        DatePublished = datePublished;
        Rating = Math.Round(rating, 2, MidpointRounding.AwayFromZero);
        IsDeleted = false;
        CreatedAt = DateTime.UtcNow;
    }

    public void UpdateBook(string title, string description, DateOnly datePublished, decimal rating)
    {
        Title = title;
        Description = description;
        DatePublished = datePublished;
        Rating = Math.Round(rating, 2, MidpointRounding.AwayFromZero);
        UpdatedAt = DateTime.UtcNow;
    }

    public void DeleteBook()
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
    }
    
    public void UpdateRating(decimal rating)
    {
        Rating = Math.Round(rating, 2, MidpointRounding.AwayFromZero);
        UpdatedAt = DateTime.UtcNow;
    }
}