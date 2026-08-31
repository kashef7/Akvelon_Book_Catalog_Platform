namespace App_Common.Common.Book;

public class BookQuery
{
    public int PageNumber { get; init;}
    public int PageSize { get; init;}
    
    public string? Title { get; init;}
    public decimal? Rating { get; init;}
    
    public string? AuthorName { get; init;}
    
    public DateOnly? StartDatePublished { get; init;}
    public DateOnly? EndDatePublished { get; init;}
    

}