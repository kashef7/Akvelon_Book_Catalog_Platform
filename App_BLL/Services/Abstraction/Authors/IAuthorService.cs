using App_BLL.Common.Paging;
using App_BLL.Common.Result;
using App_BLL.Dtos.AuthorsDtos;
using App_BLL.QueryParams.Author;

namespace App_BLL.Services.Abstraction.Authors;

public interface IAuthorService
{
    Task<Result<PagedResult<AuthorGetDto>>> GetAllAuthorsAsync(AuthorQueryParams query, CancellationToken cancellationToken);
    //Get by id
    Task<Result<AuthorGetDto>> GetAuthorAsync(Guid id, CancellationToken cancellationToken);
    //Create Author
    Task<Result<Guid>> AddAuthorAsync(AuthorCreateDto Author, CancellationToken cancellationToken);
    //Update Author
    Task<Result> UpdateAuthorAsync(AuthorEditDto Author, Guid editedAuthorId, CancellationToken cancellationToken);
    //Delete Author
    Task<Result> DeleteAuthorAsync(Guid id, CancellationToken cancellationToken);
}