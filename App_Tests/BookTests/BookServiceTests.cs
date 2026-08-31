using App_BLL.Common.Result;
using App_BLL.Dtos.BooksDtos;
using App_BLL.QueryParams.Book;
using App_BLL.Services.Abstraction.Books;
using App_BLL.Services.Implementation.Books;
using App_Common.Common.Book;
using App_DAL.Entities.Authors;
using App_DAL.Entities.Books;
using App_DAL.Repos.Abstraction.Authors;
using App_DAL.Repos.Abstraction.Books;
using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace App_Tests.BookTests;

public class BookServiceTests
{
    private readonly Mock<IBookRepo> _bookRepoMock;
    private readonly Mock<IAuthorRepo> _authorRepoMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly IBookService _bookService;
    private readonly DateOnly _today = DateOnly.FromDateTime(DateTime.UtcNow);

    public BookServiceTests()
    {
        _bookRepoMock = new Mock<IBookRepo>();
        _authorRepoMock = new Mock<IAuthorRepo>();
        _mapperMock = new Mock<IMapper>();
        _bookService = new BookService(_bookRepoMock.Object, _authorRepoMock.Object, _mapperMock.Object, new NullLogger<BookService>());
    }

    //test GetAllBooksAsync return correct data without filters
    [Fact]
    public async Task GetAllBooksAsync_NoFiltersPassed_ReturnsAllBooks()
    {
        //Arrange
        var bookQuery = new BookQueryParams();
        var author1 = new Author("Author1");
        var author2 = new Author("Author2");
        var author3 = new Author("Author3");
        IReadOnlyList<Book> books = new List<Book>()
        {
            new Book("9780132350884", "Book1", "The 1st book", author1, _today, 5),
            new Book("9780201633610", "Book2", "The 2nd book", author2, _today, 5),
            new Book("9780201485677", "Book3", "The 3rd book", author3, _today, 5),
        };
        int totalCount = books.Count;

        var mappedQuery = new BookQuery();
        IReadOnlyList<BookGetDto> bookDto = new List<BookGetDto>()
        {
            new BookGetDto { Id = Guid.NewGuid(), Title = "Book1", Description = "The 1st book", AuthorName = "Author1", DatePublished = _today, Rating = 5 },
            new BookGetDto { Id = Guid.NewGuid(), Title = "Book2", Description = "The 2nd book", AuthorName = "Author2", DatePublished = _today, Rating = 5 },
            new BookGetDto { Id = Guid.NewGuid(), Title = "Book3", Description = "The 3rd book", AuthorName = "Author3", DatePublished = _today, Rating = 5 },
        };

        _bookRepoMock.Setup(repo => repo.GetAllBooksAsync(It.IsAny<BookQuery>())).ReturnsAsync((books, totalCount));
        _mapperMock.Setup(m => m.Map<BookQuery>(bookQuery)).Returns(mappedQuery);
        _mapperMock.Setup(m => m.Map<IReadOnlyList<BookGetDto>>(books)).Returns(bookDto);

        //Act
        var result = await _bookService.GetAllBooksAsync(bookQuery);

        //Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(totalCount, result.Data!.Items.Count);
        Assert.Equal(bookDto, result.Data.Items);
        Assert.Equal(bookQuery.PageNumber, result.Data.PageNumber);
        Assert.Equal(bookQuery.PageSize, result.Data.PageSize);
        _mapperMock.Verify(m => m.Map<BookQuery>(bookQuery), Times.Once);
        _mapperMock.Verify(m => m.Map<IReadOnlyList<BookGetDto>>(books), Times.Once);
        _bookRepoMock.Verify(repo => repo.GetAllBooksAsync(mappedQuery), Times.Once);
    }
    
    //test GetAllBooksAsync maps and forwards filter values to the repo unchanged
    [Fact]
    public async Task GetAllBooksAsync_FiltersPassed_ForwardsMappedQueryToRepo()
    {
        //Arrange
        var bookQuery = new BookQueryParams
        {
            MinRating = 4.0m,
            Title = "Book1"
        };
        var author1 = new Author("Author1");
        IReadOnlyList<Book> books = new List<Book>()
        {
            new Book("9780132350884", "Book1", "The 1st book", author1, _today, 5),
        };
        int totalCount = books.Count;

        var mappedQuery = new BookQuery { Title = "Book1", MinRating = 4.0m };
        IReadOnlyList<BookGetDto> bookDto = new List<BookGetDto>()
        {
            new BookGetDto { Id = Guid.NewGuid(), Title = "Book1", Description = "The 1st book", AuthorName = "Author1", DatePublished = _today, Rating = 5 },
        };

        _bookRepoMock.Setup(repo => repo.GetAllBooksAsync(It.IsAny<BookQuery>())).ReturnsAsync((books, totalCount));
        _mapperMock.Setup(m => m.Map<BookQuery>(bookQuery)).Returns(mappedQuery);
        _mapperMock.Setup(m => m.Map<IReadOnlyList<BookGetDto>>(books)).Returns(bookDto);

        //Act
        var result = await _bookService.GetAllBooksAsync(bookQuery);

        //Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(bookDto, result.Data!.Items);
        _mapperMock.Verify(m => m.Map<BookQuery>(bookQuery), Times.Once);
        _bookRepoMock.Verify(repo => repo.GetAllBooksAsync(mappedQuery), Times.Once);
    }

    //test GetBookAsync returns book when the id is found
    [Fact]
    public async Task GetBookAsync_BookFound_ReturnsMappedBook()
    {
        //Arrange
        var author = new Author("Author1");
        var book = new Book("9780132350884", "Book1", "The 1st book", author, _today, 5);
        var bookDto = new BookGetDto { Id = book.Id, Title = "Book1", Description = "The 1st book", AuthorName = "Author1", DatePublished = _today, Rating = 5 };

        _bookRepoMock.Setup(repo => repo.GetBookByIdAsync(book.Id)).ReturnsAsync(book);
        _mapperMock.Setup(m => m.Map<BookGetDto>(book)).Returns(bookDto);

        //Act
        var result = await _bookService.GetBookAsync(book.Id);

        //Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(bookDto, result.Data);
    }

    //test GetBookAsync returns not found when book with id not found
    [Fact]
    public async Task GetBookAsync_BookNotFound_ReturnsNotFound()
    {
        //Arrange
        var bookId = Guid.NewGuid();
        _bookRepoMock.Setup(repo => repo.GetBookByIdAsync(bookId)).ReturnsAsync((Book?)null);

        //Act
        var result = await _bookService.GetBookAsync(bookId);

        //Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.Error);
    }

    //test GetBookByIsbnAsync returns book when isbn is found
    [Fact]
    public async Task GetBookByIsbnAsync_BookFound_ReturnsMappedBook()
    {
        //Arrange
        var author = new Author("Author1");
        var book = new Book("9780132350884", "Book1", "The 1st book", author, _today, 5);
        var bookDto = new BookGetDto { Id = book.Id, Title = "Book1", Description = "The 1st book", AuthorName = "Author1", DatePublished = _today, Rating = 5 };

        _bookRepoMock.Setup(repo => repo.GetBookByIsbnAsync(book.Isbn)).ReturnsAsync(book);
        _mapperMock.Setup(m => m.Map<BookGetDto>(book)).Returns(bookDto);

        //Act
        var result = await _bookService.GetBookByIsbnAsync(book.Isbn);

        //Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(bookDto, result.Data);
    }

    //test GetBookByIsbnAsync returns not found when book with isbn not found
    [Fact]
    public async Task GetBookByIsbnAsync_BookNotFound_ReturnsNotFound()
    {
        //Arrange
        var isbn = "9780132350884";
        _bookRepoMock.Setup(repo => repo.GetBookByIsbnAsync(isbn)).ReturnsAsync((Book?)null);

        //Act
        var result = await _bookService.GetBookByIsbnAsync(isbn);

        //Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.Error);
    }

    //test AddBookAsync runs correctly with correct book data
    [Fact]
    public async Task AddBookAsync_ValidBook_ReturnsSuccessWithId()
    {
        //Arrange
        var author = new Author("Author1");
        var createDto = new BookCreateDto
        {
            Title = "Book1", Description = "The 1st book", Isbn = "9780132350884", AuthorId = author.Id,
            DatePublished = _today, Rating = 5
        };
        _authorRepoMock.Setup(repo => repo.GetAuthorByIdAsync(author.Id)).ReturnsAsync(author);
        _bookRepoMock.Setup(repo => repo.AddBookAsync(It.IsAny<Book>())).Returns(Task.CompletedTask);

        //Act
        var result = await _bookService.AddBookAsync(createDto);

        //Assert
        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Data);
        _bookRepoMock.Verify(repo => repo.AddBookAsync(It.Is<Book>(b => b.Title == createDto.Title && b.AuthorId == author.Id)), Times.Once);
    }

    //test AddBookAsync returns not found when author does not exist
    [Fact]
    public async Task AddBookAsync_AuthorNotFound_ReturnsNotFound()
    {
        //Arrange
        var authorId = Guid.NewGuid();
        var createDto = new BookCreateDto
        {
            Title = "Book1", Description = "The 1st book", Isbn = "9780132350884", AuthorId = authorId,
            DatePublished = _today, Rating = 5
        };
        _authorRepoMock.Setup(repo => repo.GetAuthorByIdAsync(authorId)).ReturnsAsync((Author?)null);

        //Act
        var result = await _bookService.AddBookAsync(createDto);

        //Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.Error);
        _bookRepoMock.Verify(repo => repo.AddBookAsync(It.IsAny<Book>()), Times.Never);
    }

    //test AddBookAsync returns bad request when Date published is in the Future
    [Theory]
    [InlineData(1)]
    [InlineData(30)]
    [InlineData(365)]
    public async Task AddBookAsync_DatePublishedInFuture_ReturnsBadRequest(int daysAhead)
    {
        //Arrange
        var futureDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(daysAhead));
        var createDto = new BookCreateDto
        {
            Title = "Book1", Description = "The 1st book", Isbn = "9780132350884", AuthorId = Guid.NewGuid(),
            DatePublished = futureDate, Rating = 5
        };

        //Act
        var result = await _bookService.AddBookAsync(createDto);

        //Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.BadRequest, result.Error);
        _bookRepoMock.Verify(repo => repo.AddBookAsync(It.IsAny<Book>()), Times.Never);
    }

    //test UpdateBookAsync runs correctly with correct book data
    [Fact]
    public async Task UpdateBookAsync_ValidBook_ReturnsSuccess()
    {
        //Arrange
        var author = new Author("OldAuthor");
        var existingBook = new Book("9780132350884", "OldTitle", "OldDesc", author, _today, 3);
        var editDto = new BookEditDto
        {
            Title = "NewTitle", Description = "NewDesc",
            DatePublished = _today, Rating = 4
        };
        _bookRepoMock.Setup(repo => repo.GetBookByIdAsync(existingBook.Id)).ReturnsAsync(existingBook);

        //Act
        var result = await _bookService.UpdateBookAsync(editDto, existingBook.Id);

        //Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("NewTitle", existingBook.Title);
        _bookRepoMock.Verify(repo => repo.SaveChangesAsync(), Times.Once);
    }

    //test UpdateBookAsync returns Not Found if book not found
    [Fact]
    public async Task UpdateBookAsync_BookNotFound_ReturnsNotFound()
    {
        //Arrange
        var bookId = Guid.NewGuid();
        var editDto = new BookEditDto
        {
            Title = "NewTitle", Description = "NewDesc",
            DatePublished = _today, Rating = 4
        };
        _bookRepoMock.Setup(repo => repo.GetBookByIdAsync(bookId)).ReturnsAsync((Book?)null);

        //Act
        var result = await _bookService.UpdateBookAsync(editDto, bookId);

        //Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.Error);
    }

    //test UpdateBookAsync returns Not Found if book is deleted
    [Fact]
    public async Task UpdateBookAsync_BookIsDeleted_ReturnsNotFound()
    {
        //Arrange
        var author = new Author("Author");
        var deletedBook = new Book("9780132350884", "Title", "Desc", author, _today, 3);
        deletedBook.DeleteBook();
        var editDto = new BookEditDto
        {
            Title = "NewTitle", Description = "NewDesc",
            DatePublished = _today, Rating = 4
        };
        _bookRepoMock.Setup(repo => repo.GetBookByIdAsync(deletedBook.Id)).ReturnsAsync(deletedBook);

        //Act
        var result = await _bookService.UpdateBookAsync(editDto, deletedBook.Id);

        //Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.Error);
    }

    //test returns bad request when Date published is in the Future
    [Theory]
    [InlineData(1)]
    [InlineData(30)]
    [InlineData(365)]
    public async Task UpdateBookAsync_DatePublishedInFuture_ReturnsBadRequest(int daysAhead)
    {
        //Arrange
        var author = new Author("Author");
        var existingBook = new Book("9780132350884", "Title", "Desc", author, _today, 3);
        var futureDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(daysAhead));
        var editDto = new BookEditDto
        {
            Title = "NewTitle", Description = "NewDesc",
            DatePublished = futureDate, Rating = 4
        };
        _bookRepoMock.Setup(repo => repo.GetBookByIdAsync(existingBook.Id)).ReturnsAsync(existingBook);

        //Act
        var result = await _bookService.UpdateBookAsync(editDto, existingBook.Id);

        //Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.BadRequest, result.Error);
        Assert.Equal("Title", existingBook.Title); // proves the book was NOT mutated
    }

    //test UpdateBookRatingAsync runs correctly with correct book data
    [Fact]
    public async Task UpdateBookRatingAsync_ValidRating_ReturnsSuccess()
    {
        //Arrange
        var author = new Author("Author");
        var existingBook = new Book("9780132350884", "Title", "Desc", author, _today, 3);
        _bookRepoMock.Setup(repo => repo.GetBookByIdAsync(existingBook.Id)).ReturnsAsync(existingBook);

        //Act
        var result = await _bookService.UpdateBookRatingAsync(existingBook.Id, 4.5m);

        //Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(4.5m, existingBook.Rating);
        _bookRepoMock.Verify(repo => repo.SaveChangesAsync(), Times.Once);
    }

    //test UpdateBookRatingAsync returns Not Found if book not found
    [Fact]
    public async Task UpdateBookRatingAsync_BookNotFound_ReturnsNotFound()
    {
        //Arrange
        var bookId = Guid.NewGuid();
        _bookRepoMock.Setup(repo => repo.GetBookByIdAsync(bookId)).ReturnsAsync((Book?)null);

        //Act
        var result = await _bookService.UpdateBookRatingAsync(bookId, 4.5m);

        //Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.Error);
    }

    //test UpdateBookRatingAsync returns Not Found if book is deleted
    [Fact]
    public async Task UpdateBookRatingAsync_BookIsDeleted_ReturnsNotFound()
    {
        //Arrange
        var author = new Author("Author");
        var deletedBook = new Book("9780132350884", "Title", "Desc", author, _today, 3);
        deletedBook.DeleteBook();
        _bookRepoMock.Setup(repo => repo.GetBookByIdAsync(deletedBook.Id)).ReturnsAsync(deletedBook);

        //Act
        var result = await _bookService.UpdateBookRatingAsync(deletedBook.Id, 4.5m);

        //Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.Error);
    }

    //test DeleteBookAsync runs correctly with correct book data
    [Fact]
    public async Task DeleteBookAsync_ValidBook_ReturnsSuccess()
    {
        //Arrange
        var author = new Author("Author");
        var existingBook = new Book("9780132350884", "Title", "Desc", author, _today, 3);
        _bookRepoMock.Setup(repo => repo.GetBookByIdAsync(existingBook.Id)).ReturnsAsync(existingBook);

        //Act
        var result = await _bookService.DeleteBookAsync(existingBook.Id);

        //Assert
        Assert.True(result.IsSuccess);
        Assert.True(existingBook.IsDeleted);
        _bookRepoMock.Verify(repo => repo.SaveChangesAsync(), Times.Once);
    }

    //test DeleteBookAsync returns Not Found if book not found
    [Fact]
    public async Task DeleteBookAsync_BookNotFound_ReturnsNotFound()
    {
        //Arrange
        var bookId = Guid.NewGuid();
        _bookRepoMock.Setup(repo => repo.GetBookByIdAsync(bookId)).ReturnsAsync((Book?)null);

        //Act
        var result = await _bookService.DeleteBookAsync(bookId);

        //Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.Error);
    }

    //test DeleteBookAsync returns Not Found if book is deleted
    [Fact]
    public async Task DeleteBookAsync_BookIsDeleted_ReturnsNotFound()
    {
        //Arrange
        var author = new Author("Author");
        var deletedBook = new Book("9780132350884", "Title", "Desc", author, _today, 3);
        deletedBook.DeleteBook();
        _bookRepoMock.Setup(repo => repo.GetBookByIdAsync(deletedBook.Id)).ReturnsAsync(deletedBook);

        //Act
        var result = await _bookService.DeleteBookAsync(deletedBook.Id);

        //Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.Error);
    }
}