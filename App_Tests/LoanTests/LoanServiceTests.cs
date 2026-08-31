using App_BLL.Common.Result;
using App_BLL.Dtos.LoansDtos;
using App_BLL.QueryParams.Loan;
using App_BLL.Services.Abstraction.Loans;
using App_BLL.Services.Implementation.Loans;
using App_Common.Common.Loan;
using App_DAL.Entities.Authors;
using App_DAL.Entities.Books;
using App_DAL.Entities.Loans;
using App_DAL.Entities.Users;
using App_DAL.Repos.Abstraction.Books;
using App_DAL.Repos.Abstraction.Loans;
using App_DAL.Repos.Abstraction.Users;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace App_Tests.LoanTests;

public class LoanServiceTests
{
    private readonly Mock<ILoanRepo> _loanRepoMock;
    private readonly Mock<IUserRepo> _userRepoMock;
    private readonly Mock<IBookRepo> _bookRepoMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly ILoanService _loanService;
    private readonly DateOnly _today = DateOnly.FromDateTime(DateTime.UtcNow);

    public LoanServiceTests()
    {
        _loanRepoMock = new Mock<ILoanRepo>();
        _userRepoMock = new Mock<IUserRepo>();
        _bookRepoMock = new Mock<IBookRepo>();
        _mapperMock = new Mock<IMapper>();
        _loanService = new LoanService(
            _loanRepoMock.Object,
            _userRepoMock.Object,
            _bookRepoMock.Object,
            _mapperMock.Object,
            new NullLogger<LoanService>());
    }

    private (Book book, User user, Loan loan) CreateSampleLoan(DateTime dueAt)
    {
        var author = new Author("Sample Author");
        var book = new Book("9780132350884", "Sample Book", "Description", author, _today, 4.5m);
        var user = new User("Sample User");
        var loan = new Loan(dueAt, book, user);
        return (book, user, loan);
    }

    //test GetLoansAsync return correct data without filters
    [Fact]
    public async Task GetLoansAsync_NoFiltersPassed_ReturnsAllLoans()
    {
        //Arrange
        var loanQuery = new LoanQueryParams();
        var dueAt = DateTime.UtcNow.AddDays(14);
        var (_, _, loan1) = CreateSampleLoan(dueAt);
        var (_, _, loan2) = CreateSampleLoan(dueAt);
        IReadOnlyList<Loan> loans = new List<Loan> { loan1, loan2 };
        int totalCount = loans.Count;

        var mappedQuery = new LoanQuery();
        IReadOnlyList<LoanGetDto> loanDtos = new List<LoanGetDto>
        {
            new LoanGetDto { Id = loan1.Id, BookTitle = "Book 1", UserName = "User 1", DueAt = dueAt },
            new LoanGetDto { Id = loan2.Id, BookTitle = "Book 2", UserName = "User 2", DueAt = dueAt }
        };

        _loanRepoMock.Setup(repo => repo.GetAllLoansAsync(It.IsAny<LoanQuery>())).ReturnsAsync((loans, totalCount));
        _mapperMock.Setup(m => m.Map<LoanQuery>(loanQuery)).Returns(mappedQuery);
        _mapperMock.Setup(m => m.Map<IReadOnlyList<LoanGetDto>>(loans)).Returns(loanDtos);

        //Act
        var result = await _loanService.GetLoansAsync(loanQuery);

        //Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(totalCount, result.Data!.Items.Count);
        Assert.Equal(loanDtos, result.Data.Items);
        Assert.Equal(loanQuery.PageNumber, result.Data.PageNumber);
        Assert.Equal(loanQuery.PageSize, result.Data.PageSize);
        _mapperMock.Verify(m => m.Map<LoanQuery>(loanQuery), Times.Once);
        _mapperMock.Verify(m => m.Map<IReadOnlyList<LoanGetDto>>(loans), Times.Once);
        _loanRepoMock.Verify(repo => repo.GetAllLoansAsync(mappedQuery), Times.Once);
    }

    //test GetLoansAsync maps and forwards filter values to the repo unchanged
    [Fact]
    public async Task GetLoansAsync_FiltersPassed_ForwardsMappedQueryToRepo()
    {
        //Arrange
        var bookId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var loanQuery = new LoanQueryParams
        {
            BookId = bookId,
            UserId = userId,
            IsReturned = false
        };
        var dueAt = DateTime.UtcNow.AddDays(14);
        var (_, _, loan1) = CreateSampleLoan(dueAt);
        IReadOnlyList<Loan> loans = new List<Loan> { loan1 };
        int totalCount = loans.Count;

        var mappedQuery = new LoanQuery
        {
            BookId = bookId,
            UserId = userId,
            IsReturned = false
        };
        IReadOnlyList<LoanGetDto> loanDtos = new List<LoanGetDto>
        {
            new LoanGetDto { Id = loan1.Id, BookTitle = "Book 1", UserName = "User 1", DueAt = dueAt }
        };

        _loanRepoMock.Setup(repo => repo.GetAllLoansAsync(It.IsAny<LoanQuery>())).ReturnsAsync((loans, totalCount));
        _mapperMock.Setup(m => m.Map<LoanQuery>(loanQuery)).Returns(mappedQuery);
        _mapperMock.Setup(m => m.Map<IReadOnlyList<LoanGetDto>>(loans)).Returns(loanDtos);

        //Act
        var result = await _loanService.GetLoansAsync(loanQuery);

        //Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(loanDtos, result.Data!.Items);
        _mapperMock.Verify(m => m.Map<LoanQuery>(loanQuery), Times.Once);
        _loanRepoMock.Verify(repo => repo.GetAllLoansAsync(mappedQuery), Times.Once);
    }

    //test GetLoanByIdAsync returns loan and mapped LoanId matches loan Id
    [Fact]
    public async Task GetLoanByIdAsync_LoanFound_ReturnsMappedLoanWithMatchingId()
    {
        //Arrange
        var dueAt = DateTime.UtcNow.AddDays(14);
        var (_, _, loan) = CreateSampleLoan(dueAt);
        var loanDto = new LoanGetDto
        {
            Id = loan.Id,
            BookTitle = loan.Book.Title,
            UserName = loan.User.Name,
            DueAt = dueAt
        };

        _loanRepoMock.Setup(repo => repo.GetLoanByIdAsync(loan.Id)).ReturnsAsync(loan);
        _mapperMock.Setup(m => m.Map<LoanGetDto>(loan)).Returns(loanDto);

        //Act
        var result = await _loanService.GetLoanByIdAsync(loan.Id);

        //Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(loan.Id, result.Data!.Id);
        _loanRepoMock.Verify(repo => repo.GetLoanByIdAsync(loan.Id), Times.Once);
    }

    //test GetLoanByIdAsync returns not found when loan does not exist
    [Fact]
    public async Task GetLoanByIdAsync_LoanNotFound_ReturnsNotFound()
    {
        //Arrange
        var loanId = Guid.NewGuid();
        _loanRepoMock.Setup(repo => repo.GetLoanByIdAsync(loanId)).ReturnsAsync((Loan?)null);

        //Act
        var result = await _loanService.GetLoanByIdAsync(loanId);

        //Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.Error);
    }

    //test LoanBookAsync returns not found when book does not exist
    [Fact]
    public async Task LoanBookAsync_BookNotFound_ReturnsNotFound()
    {
        //Arrange
        var bookId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var loanCreateDto = new LoanCreateDto
        {
            BookId = bookId,
            UserId = userId,
            DueAt = DateTime.UtcNow.AddDays(14)
        };

        _bookRepoMock.Setup(repo => repo.GetBookByIdAsync(bookId)).ReturnsAsync((Book?)null);

        //Act
        var result = await _loanService.LoanBookAsync(loanCreateDto);

        //Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.Error);
        _loanRepoMock.Verify(repo => repo.AddLoanAsync(It.IsAny<Loan>()), Times.Never);
    }

    //test LoanBookAsync returns not found when user does not exist
    [Fact]
    public async Task LoanBookAsync_UserNotFound_ReturnsNotFound()
    {
        //Arrange
        var dueAt = DateTime.UtcNow.AddDays(14);
        var (book, _, _) = CreateSampleLoan(dueAt);
        var userId = Guid.NewGuid();
        var loanCreateDto = new LoanCreateDto
        {
            BookId = book.Id,
            UserId = userId,
            DueAt = dueAt
        };

        _bookRepoMock.Setup(repo => repo.GetBookByIdAsync(book.Id)).ReturnsAsync(book);
        _userRepoMock.Setup(repo => repo.GetUserByIdAsync(userId)).ReturnsAsync((User?)null);

        //Act
        var result = await _loanService.LoanBookAsync(loanCreateDto);

        //Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.Error);
        _loanRepoMock.Verify(repo => repo.AddLoanAsync(It.IsAny<Loan>()), Times.Never);
    }

    //test LoanBookAsync returns bad request when due date is in the past
    [Fact]
    public async Task LoanBookAsync_DueAtInPast_ReturnsBadRequest()
    {
        //Arrange
        var pastDueAt = DateTime.UtcNow.AddDays(-1);
        var (book, user, _) = CreateSampleLoan(DateTime.UtcNow.AddDays(14));
        var loanCreateDto = new LoanCreateDto
        {
            BookId = book.Id,
            UserId = user.Id,
            DueAt = pastDueAt
        };

        _bookRepoMock.Setup(repo => repo.GetBookByIdAsync(book.Id)).ReturnsAsync(book);
        _userRepoMock.Setup(repo => repo.GetUserByIdAsync(user.Id)).ReturnsAsync(user);

        //Act
        var result = await _loanService.LoanBookAsync(loanCreateDto);

        //Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.BadRequest, result.Error);
        _loanRepoMock.Verify(repo => repo.AddLoanAsync(It.IsAny<Loan>()), Times.Never);
    }

    //test LoanBookAsync returns conflict when book is already loaned
    [Fact]
    public async Task LoanBookAsync_BookAlreadyLoaned_ReturnsConflict()
    {
        //Arrange
        var dueAt = DateTime.UtcNow.AddDays(14);
        var (book, user, _) = CreateSampleLoan(dueAt);
        var loanCreateDto = new LoanCreateDto
        {
            BookId = book.Id,
            UserId = user.Id,
            DueAt = dueAt
        };

        _bookRepoMock.Setup(repo => repo.GetBookByIdAsync(book.Id)).ReturnsAsync(book);
        _userRepoMock.Setup(repo => repo.GetUserByIdAsync(user.Id)).ReturnsAsync(user);
        _loanRepoMock.Setup(repo => repo.HasActiveLoanAsync(book.Id)).ReturnsAsync(true);

        //Act
        var result = await _loanService.LoanBookAsync(loanCreateDto);

        //Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.Error);
        _loanRepoMock.Verify(repo => repo.AddLoanAsync(It.IsAny<Loan>()), Times.Never);
    }

    //test LoanBookAsync catches DbUpdateException and returns conflict on concurrent loan race condition
    [Fact]
    public async Task LoanBookAsync_ConcurrentLoanRaceCondition_ReturnsConflict()
    {
        //Arrange
        var dueAt = DateTime.UtcNow.AddDays(14);
        var (book, user, _) = CreateSampleLoan(dueAt);
        var loanCreateDto = new LoanCreateDto
        {
            BookId = book.Id,
            UserId = user.Id,
            DueAt = dueAt
        };

        _bookRepoMock.Setup(repo => repo.GetBookByIdAsync(book.Id)).ReturnsAsync(book);
        _userRepoMock.Setup(repo => repo.GetUserByIdAsync(user.Id)).ReturnsAsync(user);
        _loanRepoMock.Setup(repo => repo.HasActiveLoanAsync(book.Id)).ReturnsAsync(false);
        _loanRepoMock.Setup(repo => repo.AddLoanAsync(It.IsAny<Loan>())).ThrowsAsync(new DbUpdateException());

        //Act
        var result = await _loanService.LoanBookAsync(loanCreateDto);

        //Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.Error);
    }

    //test LoanBookAsync returns success with new loan Id on valid loan request
    [Fact]
    public async Task LoanBookAsync_ValidLoan_ReturnsSuccessWithId()
    {
        //Arrange
        var dueAt = DateTime.UtcNow.AddDays(14);
        var (book, user, _) = CreateSampleLoan(dueAt);
        var loanCreateDto = new LoanCreateDto
        {
            BookId = book.Id,
            UserId = user.Id,
            DueAt = dueAt
        };

        _bookRepoMock.Setup(repo => repo.GetBookByIdAsync(book.Id)).ReturnsAsync(book);
        _userRepoMock.Setup(repo => repo.GetUserByIdAsync(user.Id)).ReturnsAsync(user);
        _loanRepoMock.Setup(repo => repo.HasActiveLoanAsync(book.Id)).ReturnsAsync(false);
        _loanRepoMock.Setup(repo => repo.AddLoanAsync(It.IsAny<Loan>())).Returns(Task.CompletedTask);

        //Act
        var result = await _loanService.LoanBookAsync(loanCreateDto);

        //Assert
        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Data);
        _loanRepoMock.Verify(repo => repo.AddLoanAsync(It.Is<Loan>(l => l.BookId == book.Id && l.UserId == user.Id)), Times.Once);
    }

    //test ReturnBookAsync returns not found if loan not found
    [Fact]
    public async Task ReturnBookAsync_LoanNotFound_ReturnsNotFound()
    {
        //Arrange
        var loanId = Guid.NewGuid();
        _loanRepoMock.Setup(repo => repo.GetLoanByIdAsync(loanId)).ReturnsAsync((Loan?)null);

        //Act
        var result = await _loanService.ReturnBookAsync(loanId);

        //Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.Error);
    }

    //test ReturnBookAsync returns bad request if book is already returned
    [Fact]
    public async Task ReturnBookAsync_BookAlreadyReturned_ReturnsBadRequest()
    {
        //Arrange
        var dueAt = DateTime.UtcNow.AddDays(14);
        var (_, _, loan) = CreateSampleLoan(dueAt);
        loan.ReturnBook();
        _loanRepoMock.Setup(repo => repo.GetLoanByIdAsync(loan.Id)).ReturnsAsync(loan);

        //Act
        var result = await _loanService.ReturnBookAsync(loan.Id);

        //Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.BadRequest, result.Error);
        _loanRepoMock.Verify(repo => repo.SaveChangesAsync(), Times.Never);
    }

    //test ReturnBookAsync sets ReturnedAt and saves changes on success
    [Fact]
    public async Task ReturnBookAsync_ValidLoan_ReturnsSuccess()
    {
        //Arrange
        var dueAt = DateTime.UtcNow.AddDays(14);
        var (_, _, loan) = CreateSampleLoan(dueAt);
        _loanRepoMock.Setup(repo => repo.GetLoanByIdAsync(loan.Id)).ReturnsAsync(loan);

        //Act
        var result = await _loanService.ReturnBookAsync(loan.Id);

        //Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(loan.ReturnedAt);
        _loanRepoMock.Verify(repo => repo.SaveChangesAsync(), Times.Once);
    }
}
