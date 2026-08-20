using App_Common.Common.Book;

namespace App_DAL.Entities.Books;

public class Book
{
    public Guid Id { get; init; } = Guid.NewGuid(); //When adding the DB check for a better option for Indexing
    public string Title { get; private set; }
    public string Description { get; private set; }
    public string AuthorName { get; private set; }
    public DateOnly DatePublished { get; private set; }
    public decimal Rating { get; private set; }
    
    public BookStatus Status { get; private set; }

    public bool IsDeleted { get; private set; }
    
    public Book(string title, string description, string authorName, DateOnly datePublished, decimal rating ,  BookStatus status)
    {
        Title = title;
        Description = description;
        AuthorName = authorName;
        DatePublished = datePublished;
        Rating = rating;
        IsDeleted = false;
        Status = status;
    }

    public void UpdateBook(string title, string description, string authorName, DateOnly datePublished, decimal rating,BookStatus status)
    {
        Title = title;
        Description = description;
        AuthorName = authorName;
        DatePublished = datePublished;
        Rating = rating;
        Status = status;
    }

    public void DeleteBook()
    {
        IsDeleted = true;
    }
    
    public void UpdateRating(decimal rating)
    {
        Rating = rating;
    }

    public void UpdateStatus(BookStatus status)
    {
        Status = status;
    }
}