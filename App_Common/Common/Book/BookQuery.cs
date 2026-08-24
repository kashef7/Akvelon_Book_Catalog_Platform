namespace App_Common.Common.Book;

public class BookQuery
{
    public int PageNumber { get; init;}
    public int PageSize { get; init;}
    
    public string? Title { get; init;}
    public BookStatus? Status { get; init;}
    public decimal? Rating { get; init;}
    

}