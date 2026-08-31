namespace App_Common.Common.Book;

public class BookQuery
{
    public int PageNumber { get; init;}
    public int PageSize { get; init;}
    
    public string? Title { get; init;}
    public string? Isbn { get; init; }
    public Guid? AuthorId { get; init; }
    public decimal? MinRating { get; init; }
    public decimal? MaxRating { get; init; }
    
    public string? AuthorName { get; init;}
    
    public DateOnly? StartDatePublished { get; init;}
    public DateOnly? EndDatePublished { get; init;}
    

}