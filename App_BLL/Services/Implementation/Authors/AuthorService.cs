using App_BLL.Common.Paging;
using App_BLL.Common.Result;
using App_BLL.Dtos.AuthorsDtos;
using App_BLL.QueryParams.Author;
using App_BLL.Services.Abstraction.Authors;
using App_Common.Common.Author;
using App_DAL.Entities.Authors;
using App_DAL.Repos.Abstraction.Authors;
using App_DAL.Repos.Abstraction.Books;
using AutoMapper;
using Microsoft.Extensions.Logging;

namespace App_BLL.Services.Implementation.Authors;

public class AuthorService : IAuthorService
{
    private readonly IAuthorRepo _authorRepo;
    private readonly IBookRepo _bookRepo;
    private readonly IMapper _mapper;
    private readonly ILogger<AuthorService> _logger;

    public AuthorService(IAuthorRepo authorRepo,IBookRepo bookRepo ,IMapper mapper, ILogger<AuthorService> logger)
    {
        _authorRepo = authorRepo;
        _bookRepo = bookRepo;
        _mapper = mapper;
        _logger = logger;
    }
    
    public async Task<Result<PagedResult<AuthorGetDto>>> GetAllAuthorsAsync(AuthorQueryParams query, CancellationToken cancellationToken)
    {
        var authorQuery = _mapper.Map<AuthorQuery>(query);
        
        var (authors,TotalCount) = await _authorRepo.GetAllAuthorsAsync(authorQuery, cancellationToken);
        var dtos = _mapper.Map<IReadOnlyList<AuthorGetDto>>(authors);
        
        return Result<PagedResult<AuthorGetDto>>.Success(new PagedResult<AuthorGetDto>()
        {
            Items = dtos,
            TotalCount = TotalCount,
            PageNumber = query.PageNumber,
            PageSize = query.PageSize
        });
    }

    public async Task<Result<AuthorGetDto>> GetAuthorAsync(Guid id, CancellationToken cancellationToken)
    {
        var author = await _authorRepo.GetAuthorByIdAsync(id, cancellationToken);
        if (author == null)
        {
            _logger.LogWarning("Author {AuthorId} Not Found", id);
            return Result<AuthorGetDto>.Failed(ErrorType.NotFound, "Author Not Found");
        }
        return Result<AuthorGetDto>.Success(_mapper.Map<AuthorGetDto>(author));
    }

    public async Task<Result<Guid>> AddAuthorAsync(AuthorCreateDto Author, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var newAuthor = new Author(Author.Name);
        await _authorRepo.AddAuthorAsync(newAuthor);
        return Result<Guid>.Success(newAuthor.Id);
    }

    public async Task<Result> UpdateAuthorAsync(AuthorEditDto Author, Guid editedAuthorId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var editedAuthor = await _authorRepo.GetAuthorByIdAsync(editedAuthorId, cancellationToken);
        if (editedAuthor == null)
        {
            _logger.LogWarning("Updating Author Failed, Author {AuthorId} Not Found", editedAuthorId);
            return Result.Failed(ErrorType.NotFound, "Author Not Found");
        } else if (editedAuthor.IsDeleted)
        {
            _logger.LogWarning("Updating Author Failed, Author {AuthorId} is Deleted", editedAuthorId);
            return Result.Failed(ErrorType.NotFound, "Author Deleted");
        }
        editedAuthor.UpdateAuthor(Author.Name);
        await _authorRepo.SaveChangesAsync();
        return Result.Success("Author Updated");
    }
    

    public async Task<Result> DeleteAuthorAsync(Guid id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var deletedAuthor = await _authorRepo.GetAuthorByIdAsync(id, cancellationToken);
        if (deletedAuthor == null)
        {
            _logger.LogWarning("Deleting Author Failed, Author {AuthorId} Not Found", id);
            return Result.Failed(ErrorType.NotFound, "Author Not Found");
        } else if (deletedAuthor.IsDeleted)
        {
            _logger.LogWarning("Deleting Author Failed, Author {AuthorId} is Deleted", id);
            return Result.Failed(ErrorType.NotFound, "Author Deleted");
        }

        if (await _bookRepo.HasActiveBookByAuthorAsync(id, cancellationToken))
        {
            _logger.LogWarning("Deleting Author Failed, Author {AuthorId} has active Books", id);
            return Result.Failed(ErrorType.Conflict, "Author Has Active Book");
        }
        deletedAuthor.DeleteAuthor();
        await _authorRepo.SaveChangesAsync();
        return Result.Success("Author Deleted");
    }
}