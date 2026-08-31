using App_Common.Common.Author;
using App_DAL.Entities.Authors;

namespace App_DAL.Repos.Abstraction.Authors;

public interface IAuthorRepo
{
    Task<(IReadOnlyList<Author> items ,int TotalCount)> GetAllAuthorsAsync(AuthorQuery authorQuery);
    Task<Author?> GetAuthorByIdAsync(Guid id);
    Task AddAuthorAsync(Author author);
    Task SaveChangesAsync();
}