using App_Common.Common.Book;
using App_DAL.Entities.Books;

namespace App_DAL.Helper;

public static class BookFilters
{
    public static IQueryable<Book> ApplyQueryFilters(this IQueryable<Book> books, BookQuery bookQuery)
    {

        if (!string.IsNullOrWhiteSpace(bookQuery.Title))
        {
            books = books.Where(b => b.Title == bookQuery.Title);
        }

        if (bookQuery.Status != null)
        {
            books = books.Where(b => b.Status == bookQuery.Status);
        }

        if (bookQuery.Rating != null)
        {
            books = books.Where(b => b.Rating == bookQuery.Rating);
        }

        return books;
    }
}