using App_BLL.Dtos.BooksDtos;
using App_BLL.QueryParams.Book;
using App_BLL.Services.Abstraction.Books;
using App_BLL.Services.Implementation.Books;
using App_BLL.Common.Result;
using App_Common.Common.Book;
using App_DAL.Entities.Books;
using App_DAL.Repos.Abstraction.Books;
using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace App_Tests.BookTests;

public class BookServiceTests
{
    private readonly Mock<IBookRepo> _bookRepoMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly IBookService _bookService;

    public BookServiceTests()
    {
        _bookRepoMock = new Mock<IBookRepo>();
        _mapperMock = new Mock<IMapper>();
        _bookService = new BookService(_bookRepoMock.Object, _mapperMock.Object, new NullLogger<BookService>());
    }

    //test GetAllBooksAsync return correct data without filters
    [Fact]
    public async Task GetAllBooksAsync_NoFiltersPassed_ReturnsAllBooks()
    {
        //Arrange
        var bookQuery = new BookQueryParams();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        IReadOnlyList<Book> books = new List<Book>()
        {
            new Book("Book1", "The 1st book", "Author1", today, 5, BookStatus.NotStarted),
            new Book("Book2", "The 2nd book", "Author2", today, 5, BookStatus.NotStarted),
            new Book("Book3", "The 3rd book", "Author3", today, 5, BookStatus.NotStarted),
        };
        int totalCount = books.Count;

        var mappedQuery = new BookQuery();
        IReadOnlyList<BookGetDto> bookDto = new List<BookGetDto>()
        {
            new BookGetDto { Id = Guid.NewGuid(), Title = "Book1", Description = "The 1st book", AuthorName = "Author1", DatePublished = today, Rating = 5, Status = BookStatus.NotStarted },
            new BookGetDto { Id = Guid.NewGuid(), Title = "Book2", Description = "The 2nd book", AuthorName = "Author2", DatePublished = today, Rating = 5, Status = BookStatus.NotStarted },
            new BookGetDto { Id = Guid.NewGuid(), Title = "Book3", Description = "The 3rd book", AuthorName = "Author3", DatePublished = today, Rating = 5, Status = BookStatus.NotStarted },
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
            Rating = 5,
            Status = BookStatus.NotStarted,
            Title = "Book1"
        };
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        IReadOnlyList<Book> books = new List<Book>()
        {
            new Book("Book1", "The 1st book", "Author1", today, 5, BookStatus.NotStarted),
        };
        int totalCount = books.Count;

        var mappedQuery = new BookQuery { Title = "Book1", Status = BookStatus.NotStarted, Rating = 5 };
        IReadOnlyList<BookGetDto> bookDto = new List<BookGetDto>()
        {
            new BookGetDto { Id = Guid.NewGuid(), Title = "Book1", Description = "The 1st book", AuthorName = "Author1", DatePublished = today, Rating = 5, Status = BookStatus.NotStarted },
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
        // the important line: the repo received the SAME mapped query object, filters intact
        _bookRepoMock.Verify(repo => repo.GetAllBooksAsync(mappedQuery), Times.Once);
    }

    //test GetBookAsync returns book when the id is found
    [Fact]
    public async Task GetBookAsync_BookFound_ReturnsMappedBook()
    {
        //Arrange
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var book = new Book("Book1", "The 1st book", "Author1", today, 5, BookStatus.NotStarted);
        var bookDto = new BookGetDto { Id = book.Id, Title = "Book1", Description = "The 1st book", AuthorName = "Author1", DatePublished = today, Rating = 5, Status = BookStatus.NotStarted };

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

    //test AddBookAsync runs correctly with correct book data
    [Fact]
    public async Task AddBookAsync_ValidBook_ReturnsSuccessWithId()
    {
        //Arrange
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var createDto = new BookCreateDto
        {
            Title = "Book1", Description = "The 1st book", AuthorName = "Author1",
            DatePublished = today, Rating = 5, Status = BookStatus.NotStarted
        };
        var mappedBook = new Book(createDto.Title, createDto.Description, createDto.AuthorName, createDto.DatePublished, createDto.Rating, createDto.Status);

        _mapperMock.Setup(m => m.Map<Book>(createDto)).Returns(mappedBook);
        _bookRepoMock.Setup(repo => repo.AddBookAsync(mappedBook)).Returns(Task.CompletedTask);

        //Act
        var result = await _bookService.AddBookAsync(createDto);

        //Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(mappedBook.Id, result.Data);
        _bookRepoMock.Verify(repo => repo.AddBookAsync(mappedBook), Times.Once);
    }

    //test AddBookAsync returns bad request when Date published is in the Future
    [Theory]
    [InlineData(1)]
    [InlineData(30)]
    [InlineData(365)]
    public async Task AddBookAsync_DatePublishedInFuture_ReturnsBadRequest(int daysAhead)
    {
        //Arrange
        var futureDate = DateOnly.FromDateTime(DateTime.Now.AddDays(daysAhead));
        var createDto = new BookCreateDto
        {
            Title = "Book1", Description = "The 1st book", AuthorName = "Author1",
            DatePublished = futureDate, Rating = 5, Status = BookStatus.NotStarted
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
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var existingBook = new Book("OldTitle", "OldDesc", "OldAuthor", today, 3, BookStatus.NotStarted);
        var editDto = new BookEditDto
        {
            Title = "NewTitle", Description = "NewDesc", AuthorName = "NewAuthor",
            DatePublished = today, Rating = 4, Status = BookStatus.Started
        };
        _bookRepoMock.Setup(repo => repo.GetBookByIdAsync(existingBook.Id)).ReturnsAsync(existingBook);

        //Act
        var result = await _bookService.UpdateBookAsync(editDto, existingBook.Id);

        //Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("NewTitle", existingBook.Title);
        Assert.Equal(BookStatus.Started, existingBook.Status);
    }

    //test UpdateBookAsync returns Not Found if book not found
    [Fact]
    public async Task UpdateBookAsync_BookNotFound_ReturnsNotFound()
    {
        //Arrange
        var bookId = Guid.NewGuid();
        var editDto = new BookEditDto
        {
            Title = "NewTitle", Description = "NewDesc", AuthorName = "NewAuthor",
            DatePublished = DateOnly.FromDateTime(DateTime.UtcNow), Rating = 4, Status = BookStatus.Started
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
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var deletedBook = new Book("Title", "Desc", "Author", today, 3, BookStatus.NotStarted);
        deletedBook.DeleteBook();
        var editDto = new BookEditDto
        {
            Title = "NewTitle", Description = "NewDesc", AuthorName = "NewAuthor",
            DatePublished = today, Rating = 4, Status = BookStatus.Started
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
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var existingBook = new Book("Title", "Desc", "Author", today, 3, BookStatus.NotStarted);
        var futureDate = DateOnly.FromDateTime(DateTime.Now.AddDays(daysAhead));
        var editDto = new BookEditDto
        {
            Title = "NewTitle", Description = "NewDesc", AuthorName = "NewAuthor",
            DatePublished = futureDate, Rating = 4, Status = BookStatus.Started
        };
        _bookRepoMock.Setup(repo => repo.GetBookByIdAsync(existingBook.Id)).ReturnsAsync(existingBook);

        //Act
        var result = await _bookService.UpdateBookAsync(editDto, existingBook.Id);

        //Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.BadRequest, result.Error);
        Assert.Equal("Title", existingBook.Title); // proves the book was NOT mutated
    }

    //test UpdateBookStatusAsync runs correctly with correct book data
    [Fact]
    public async Task UpdateBookStatusAsync_ValidStatus_ReturnsSuccess()
    {
        //Arrange
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var existingBook = new Book("Title", "Desc", "Author", today, 3, BookStatus.NotStarted);
        var statusDto = new BookStatusDto { Status = BookStatus.Finished };
        _bookRepoMock.Setup(repo => repo.GetBookByIdAsync(existingBook.Id)).ReturnsAsync(existingBook);

        //Act
        var result = await _bookService.UpdateBookStatusAsync(existingBook.Id, statusDto);

        //Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(BookStatus.Finished, existingBook.Status);
    }

    //test UpdateBookStatusAsync returns Not Found if book not found
    [Fact]
    public async Task UpdateBookStatusAsync_BookNotFound_ReturnsNotFound()
    {
        //Arrange
        var bookId = Guid.NewGuid();
        var statusDto = new BookStatusDto { Status = BookStatus.Finished };
        _bookRepoMock.Setup(repo => repo.GetBookByIdAsync(bookId)).ReturnsAsync((Book?)null);

        //Act
        var result = await _bookService.UpdateBookStatusAsync(bookId, statusDto);

        //Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.Error);
    }

    //test UpdateBookStatusAsync returns Not Found if book is deleted
    [Fact]
    public async Task UpdateBookStatusAsync_BookIsDeleted_ReturnsNotFound()
    {
        //Arrange
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var deletedBook = new Book("Title", "Desc", "Author", today, 3, BookStatus.NotStarted);
        deletedBook.DeleteBook();
        var statusDto = new BookStatusDto { Status = BookStatus.Finished };
        _bookRepoMock.Setup(repo => repo.GetBookByIdAsync(deletedBook.Id)).ReturnsAsync(deletedBook);

        //Act
        var result = await _bookService.UpdateBookStatusAsync(deletedBook.Id, statusDto);

        //Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.Error);
    }

    //test UpdateBookRatingAsync runs correctly with correct book data
    [Fact]
    public async Task UpdateBookRatingAsync_ValidRating_ReturnsSuccess()
    {
        //Arrange
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var existingBook = new Book("Title", "Desc", "Author", today, 3, BookStatus.NotStarted);
        _bookRepoMock.Setup(repo => repo.GetBookByIdAsync(existingBook.Id)).ReturnsAsync(existingBook);

        //Act
        var result = await _bookService.UpdateBookRatingAsync(existingBook.Id, 4.5m);

        //Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(4.5m, existingBook.Rating);
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
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var deletedBook = new Book("Title", "Desc", "Author", today, 3, BookStatus.NotStarted);
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
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var existingBook = new Book("Title", "Desc", "Author", today, 3, BookStatus.NotStarted);
        _bookRepoMock.Setup(repo => repo.GetBookByIdAsync(existingBook.Id)).ReturnsAsync(existingBook);
        _bookRepoMock.Setup(repo => repo.DeleteBookAsync(existingBook.Id)).Returns(Task.CompletedTask);

        //Act
        var result = await _bookService.DeleteBookAsync(existingBook.Id);

        //Assert
        Assert.True(result.IsSuccess);
        _bookRepoMock.Verify(repo => repo.DeleteBookAsync(existingBook.Id), Times.Once);
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
        _bookRepoMock.Verify(repo => repo.DeleteBookAsync(It.IsAny<Guid>()), Times.Never);
    }

    //test DeleteBookAsync returns Not Found if book is deleted
    [Fact]
    public async Task DeleteBookAsync_BookIsDeleted_ReturnsNotFound()
    {
        //Arrange
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var deletedBook = new Book("Title", "Desc", "Author", today, 3, BookStatus.NotStarted);
        deletedBook.DeleteBook();
        _bookRepoMock.Setup(repo => repo.GetBookByIdAsync(deletedBook.Id)).ReturnsAsync(deletedBook);

        //Act
        var result = await _bookService.DeleteBookAsync(deletedBook.Id);

        //Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.Error);
        _bookRepoMock.Verify(repo => repo.DeleteBookAsync(It.IsAny<Guid>()), Times.Never);
    }
}