using App_Common.Common.Author;
using App_DAL.Entities.Authors;

namespace App_DAL.Repos.Abstraction.Authors;

public interface IAuthorRepo
{
    Task<(IReadOnlyList<Author> items ,int TotalCount)> GetAllAuthorsAsync(AuthorQuery authorQuery, CancellationToken cancellationToken);
    Task<Author?> GetAuthorByIdAsync(Guid id, CancellationToken cancellationToken);
    Task AddAuthorAsync(Author author);
    Task SaveChangesAsync();
}