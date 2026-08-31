using App_BLL.Common.Result;
using App_BLL.Dtos.AuthorsDtos;
using App_BLL.QueryParams.Author;
using App_BLL.Services.Abstraction.Authors;
using App_BLL.Services.Implementation.Authors;
using App_Common.Common.Author;
using App_DAL.Entities.Authors;
using App_DAL.Repos.Abstraction.Authors;
using App_DAL.Repos.Abstraction.Books;
using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace App_Tests.AuthorTests;

public class AuthorServiceTests
{
    private readonly Mock<IAuthorRepo> _authorRepoMock;
    private readonly Mock<IBookRepo> _bookRepoMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly IAuthorService _authorService;

    public AuthorServiceTests()
    {
        _authorRepoMock = new Mock<IAuthorRepo>();
        _bookRepoMock = new Mock<IBookRepo>();
        _mapperMock = new Mock<IMapper>();
        _authorService = new AuthorService(_authorRepoMock.Object, _bookRepoMock.Object, _mapperMock.Object, new NullLogger<AuthorService>());
    }

    //test GetAllAuthorsAsync return correct data without filters
    [Fact]
    public async Task GetAllAuthorsAsync_NoFiltersPassed_ReturnsAllAuthors()
    {
        //Arrange
        var authorQuery = new AuthorQueryParams();
        IReadOnlyList<Author> authors = new List<Author>()
        {
            new Author("Author1"),
            new Author("Author2"),
            new Author("Author3"),
        };
        int totalCount = authors.Count;

        var mappedQuery = new AuthorQuery();
        IReadOnlyList<AuthorGetDto> authorDtos = new List<AuthorGetDto>()
        {
            new AuthorGetDto { Id = Guid.NewGuid(), Name = "Author1" },
            new AuthorGetDto { Id = Guid.NewGuid(), Name = "Author2" },
            new AuthorGetDto { Id = Guid.NewGuid(), Name = "Author3" },
        };

        _authorRepoMock.Setup(repo => repo.GetAllAuthorsAsync(It.IsAny<AuthorQuery>())).ReturnsAsync((authors, totalCount));
        _mapperMock.Setup(m => m.Map<AuthorQuery>(authorQuery)).Returns(mappedQuery);
        _mapperMock.Setup(m => m.Map<IReadOnlyList<AuthorGetDto>>(authors)).Returns(authorDtos);

        //Act
        var result = await _authorService.GetAllAuthorsAsync(authorQuery);

        //Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(totalCount, result.Data!.Items.Count);
        Assert.Equal(authorDtos, result.Data.Items);
        Assert.Equal(authorQuery.PageNumber, result.Data.PageNumber);
        Assert.Equal(authorQuery.PageSize, result.Data.PageSize);
        _mapperMock.Verify(m => m.Map<AuthorQuery>(authorQuery), Times.Once);
        _mapperMock.Verify(m => m.Map<IReadOnlyList<AuthorGetDto>>(authors), Times.Once);
        _authorRepoMock.Verify(repo => repo.GetAllAuthorsAsync(mappedQuery), Times.Once);
    }

    //test GetAllAuthorsAsync maps and forwards filter values to the repo unchanged
    [Fact]
    public async Task GetAllAuthorsAsync_FiltersPassed_ForwardsMappedQueryToRepo()
    {
        //Arrange
        var authorQuery = new AuthorQueryParams
        {
            Name = "Author1"
        };
        IReadOnlyList<Author> authors = new List<Author>()
        {
            new Author("Author1"),
        };
        int totalCount = authors.Count;

        var mappedQuery = new AuthorQuery { Name = "Author1" };
        IReadOnlyList<AuthorGetDto> authorDtos = new List<AuthorGetDto>()
        {
            new AuthorGetDto { Id = Guid.NewGuid(), Name = "Author1" },
        };

        _authorRepoMock.Setup(repo => repo.GetAllAuthorsAsync(It.IsAny<AuthorQuery>())).ReturnsAsync((authors, totalCount));
        _mapperMock.Setup(m => m.Map<AuthorQuery>(authorQuery)).Returns(mappedQuery);
        _mapperMock.Setup(m => m.Map<IReadOnlyList<AuthorGetDto>>(authors)).Returns(authorDtos);

        //Act
        var result = await _authorService.GetAllAuthorsAsync(authorQuery);

        //Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(authorDtos, result.Data!.Items);
        _mapperMock.Verify(m => m.Map<AuthorQuery>(authorQuery), Times.Once);
        _authorRepoMock.Verify(repo => repo.GetAllAuthorsAsync(mappedQuery), Times.Once);
    }

    //test GetAuthorAsync returns author when the id is found
    [Fact]
    public async Task GetAuthorAsync_AuthorFound_ReturnsMappedAuthor()
    {
        //Arrange
        var author = new Author("Author1");
        var authorDto = new AuthorGetDto { Id = author.Id, Name = "Author1" };

        _authorRepoMock.Setup(repo => repo.GetAuthorByIdAsync(author.Id)).ReturnsAsync(author);
        _mapperMock.Setup(m => m.Map<AuthorGetDto>(author)).Returns(authorDto);

        //Act
        var result = await _authorService.GetAuthorAsync(author.Id);

        //Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(authorDto, result.Data);
    }

    //test GetAuthorAsync returns not found when author with id not found
    [Fact]
    public async Task GetAuthorAsync_AuthorNotFound_ReturnsNotFound()
    {
        //Arrange
        var authorId = Guid.NewGuid();
        _authorRepoMock.Setup(repo => repo.GetAuthorByIdAsync(authorId)).ReturnsAsync((Author?)null);

        //Act
        var result = await _authorService.GetAuthorAsync(authorId);

        //Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.Error);
    }

    //test AddAuthorAsync runs correctly and sets CreatedAt
    [Fact]
    public async Task AddAuthorAsync_ValidAuthor_ReturnsSuccessWithIdAndSetsCreatedAt()
    {
        //Arrange
        var createDto = new AuthorCreateDto { Name = "New Author" };
        Author? capturedAuthor = null;

        _authorRepoMock
            .Setup(repo => repo.AddAuthorAsync(It.IsAny<Author>()))
            .Callback<Author>(a => capturedAuthor = a)
            .Returns(Task.CompletedTask);

        //Act
        var result = await _authorService.AddAuthorAsync(createDto);

        //Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(capturedAuthor);
        Assert.Equal(createDto.Name, capturedAuthor!.Name);
        Assert.NotEqual(default(DateTime), capturedAuthor.CreatedAt);
        Assert.Equal(capturedAuthor.Id, result.Data);
        _authorRepoMock.Verify(repo => repo.AddAuthorAsync(It.IsAny<Author>()), Times.Once);
    }

    //test UpdateAuthorAsync runs correctly with valid author data
    [Fact]
    public async Task UpdateAuthorAsync_ValidAuthor_ReturnsSuccess()
    {
        //Arrange
        var existingAuthor = new Author("Old Name");
        var editDto = new AuthorEditDto { Name = "New Name" };
        _authorRepoMock.Setup(repo => repo.GetAuthorByIdAsync(existingAuthor.Id)).ReturnsAsync(existingAuthor);

        //Act
        var result = await _authorService.UpdateAuthorAsync(editDto, existingAuthor.Id);

        //Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("New Name", existingAuthor.Name);
        _authorRepoMock.Verify(repo => repo.SaveChangesAsync(), Times.Once);
    }

    //test UpdateAuthorAsync returns not found if author not found
    [Fact]
    public async Task UpdateAuthorAsync_AuthorNotFound_ReturnsNotFound()
    {
        //Arrange
        var authorId = Guid.NewGuid();
        var editDto = new AuthorEditDto { Name = "New Name" };
        _authorRepoMock.Setup(repo => repo.GetAuthorByIdAsync(authorId)).ReturnsAsync((Author?)null);

        //Act
        var result = await _authorService.UpdateAuthorAsync(editDto, authorId);

        //Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.Error);
    }

    //test UpdateAuthorAsync returns not found if author is deleted
    [Fact]
    public async Task UpdateAuthorAsync_AuthorIsDeleted_ReturnsNotFound()
    {
        //Arrange
        var deletedAuthor = new Author("Author Name");
        deletedAuthor.DeleteAuthor();
        var editDto = new AuthorEditDto { Name = "New Name" };
        _authorRepoMock.Setup(repo => repo.GetAuthorByIdAsync(deletedAuthor.Id)).ReturnsAsync(deletedAuthor);

        //Act
        var result = await _authorService.UpdateAuthorAsync(editDto, deletedAuthor.Id);

        //Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.Error);
    }

    //test DeleteAuthorAsync runs correctly when author has no active books
    [Fact]
    public async Task DeleteAuthorAsync_ValidAuthor_ReturnsSuccess()
    {
        //Arrange
        var existingAuthor = new Author("Author Name");
        _authorRepoMock.Setup(repo => repo.GetAuthorByIdAsync(existingAuthor.Id)).ReturnsAsync(existingAuthor);
        _bookRepoMock.Setup(repo => repo.HasActiveBookByAuthorAsync(existingAuthor.Id)).ReturnsAsync(false);

        //Act
        var result = await _authorService.DeleteAuthorAsync(existingAuthor.Id);

        //Assert
        Assert.True(result.IsSuccess);
        Assert.True(existingAuthor.IsDeleted);
        _authorRepoMock.Verify(repo => repo.SaveChangesAsync(), Times.Once);
    }

    //test DeleteAuthorAsync returns not found if author not found
    [Fact]
    public async Task DeleteAuthorAsync_AuthorNotFound_ReturnsNotFound()
    {
        //Arrange
        var authorId = Guid.NewGuid();
        _authorRepoMock.Setup(repo => repo.GetAuthorByIdAsync(authorId)).ReturnsAsync((Author?)null);

        //Act
        var result = await _authorService.DeleteAuthorAsync(authorId);

        //Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.Error);
    }

    //test DeleteAuthorAsync returns not found if author is deleted
    [Fact]
    public async Task DeleteAuthorAsync_AuthorIsDeleted_ReturnsNotFound()
    {
        //Arrange
        var deletedAuthor = new Author("Author Name");
        deletedAuthor.DeleteAuthor();
        _authorRepoMock.Setup(repo => repo.GetAuthorByIdAsync(deletedAuthor.Id)).ReturnsAsync(deletedAuthor);

        //Act
        var result = await _authorService.DeleteAuthorAsync(deletedAuthor.Id);

        //Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.Error);
    }

    //test DeleteAuthorAsync returns conflict if author has active books
    [Fact]
    public async Task DeleteAuthorAsync_AuthorHasActiveBooks_ReturnsConflict()
    {
        //Arrange
        var existingAuthor = new Author("Author Name");
        _authorRepoMock.Setup(repo => repo.GetAuthorByIdAsync(existingAuthor.Id)).ReturnsAsync(existingAuthor);
        _bookRepoMock.Setup(repo => repo.HasActiveBookByAuthorAsync(existingAuthor.Id)).ReturnsAsync(true);

        //Act
        var result = await _authorService.DeleteAuthorAsync(existingAuthor.Id);

        //Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.Error);
        Assert.False(existingAuthor.IsDeleted);
        _authorRepoMock.Verify(repo => repo.SaveChangesAsync(), Times.Never);
    }
}
