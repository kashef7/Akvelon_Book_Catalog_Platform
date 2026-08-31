using App_Common.Common.Book;
using App_DAL.Entities.Books;

namespace App_DAL.Filters.Books;

public static class BookFilters
{
    public static IQueryable<Book> ApplyQueryFilters(this IQueryable<Book> books, BookQuery bookQuery)
    {

        if (!string.IsNullOrWhiteSpace(bookQuery.Title))
        {
            books = books.Where(b => b.Title.Contains(bookQuery.Title));
        }


        if (bookQuery.Rating != null)
        {
            books = books.Where(b => b.Rating == bookQuery.Rating);
        }

        if (bookQuery.StartDatePublished != null)
        {
            books = books.Where(b => b.DatePublished >= bookQuery.StartDatePublished);
        }
        
        if (bookQuery.EndDatePublished != null)
        {
            books = books.Where(b => b.DatePublished <= bookQuery.EndDatePublished);
        }

        if (!string.IsNullOrEmpty(bookQuery.AuthorName))
        {
            books = books.Where(b => b.Author.Name.Contains(bookQuery.AuthorName));
        }

        return books;
    }
}