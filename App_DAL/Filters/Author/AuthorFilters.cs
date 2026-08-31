using App_Common.Common.Author;
using App_DAL.Entities.Authors;

namespace App_DAL.Filters.Authors;

public static class AuthorFilters
{
    public static IQueryable<Author> ApplyQueryFilters(this IQueryable<Author> authors, AuthorQuery authorQuery)
    {
        if (!string.IsNullOrEmpty(authorQuery.Name))
        {
            authors = authors.Where(a => a.Name.Contains(authorQuery.Name));
        }
        return authors;
    }
}