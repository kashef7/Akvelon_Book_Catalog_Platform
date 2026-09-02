using System.Net;
using System.Net.Http.Json;
using App_BLL.Common.Paging;
using App_BLL.Dtos.AuthorsDtos;
using App_BLL.Dtos.BooksDtos;
using App_BLL.Dtos.LoansDtos;
using App_BLL.Dtos.UsersDtos;
using App_Tests_Integration.Infrastructre;

namespace App_Tests_Integration.EndToEnd;

public class AppWorkflowApiTests : BaseIntegrationTest
{
    public AppWorkflowApiTests(ApiWebApplicationFactory factory) : base(factory)
    {
    }

    private record CreatedIdResponse(Guid Id);

    [Fact]
    public async Task FullLifecycle_PublishBookRegisterUserBorrowAndReturn_Succeeds()
    {
        //Arrange
        var authorDto = new AuthorCreateDto { Name = "George R.R. Martin" };
        var userDto = new UserCreateDto { Name = "Jon Snow" };

        //Act & Assert (Step-by-step workflow)
        // 1. POST api/author -> 201, extract AuthorId
        var authorResponse = await Client.PostAsJsonAsync("api/author", authorDto);
        Assert.Equal(HttpStatusCode.Created, authorResponse.StatusCode);
        var authorId = (await authorResponse.Content.ReadFromJsonAsync<CreatedIdResponse>())!.Id;

        // 2. POST api/books with that AuthorId -> 201, extract BookId
        var bookDto = new BookCreateDto
        {
            Title = "A Game of Thrones",
            Description = "Winter is coming.",
            Isbn = "9780553103540",
            AuthorId = authorId,
            DatePublished = new DateOnly(1996, 8, 1),
            Rating = 4.8m
        };
        var bookResponse = await Client.PostAsJsonAsync("api/books", bookDto);
        Assert.Equal(HttpStatusCode.Created, bookResponse.StatusCode);
        var bookId = (await bookResponse.Content.ReadFromJsonAsync<CreatedIdResponse>())!.Id;

        // 3. POST api/user -> 201, extract UserId
        var userResponse = await Client.PostAsJsonAsync("api/user", userDto);
        Assert.Equal(HttpStatusCode.Created, userResponse.StatusCode);
        var userId = (await userResponse.Content.ReadFromJsonAsync<CreatedIdResponse>())!.Id;

        // 4. GET api/books/{BookId} -> 200, assert it shows up as a normal, unloaned book
        var getBookResponse = await Client.GetAsync($"api/books/{bookId}");
        Assert.Equal(HttpStatusCode.OK, getBookResponse.StatusCode);
        var book = await getBookResponse.Content.ReadFromJsonAsync<BookGetDto>();
        Assert.NotNull(book);
        Assert.Equal(bookId, book.Id);
        Assert.Equal(authorDto.Name, book.AuthorName);

        // 5. POST api/loan with {BookId, UserId, DueAt} -> 201, extract LoanId
        var loanDto = new LoanCreateDto
        {
            BookId = bookId,
            UserId = userId,
            DueAt = DateTime.UtcNow.AddDays(14)
        };
        var loanResponse = await Client.PostAsJsonAsync("api/loan", loanDto);
        Assert.Equal(HttpStatusCode.Created, loanResponse.StatusCode);
        var loanId = (await loanResponse.Content.ReadFromJsonAsync<CreatedIdResponse>())!.Id;

        // 6. GET api/loan/{LoanId} -> 200, assert ReturnedAt is null and BookId/UserId match
        var getLoanResponse = await Client.GetAsync($"api/loan/{loanId}");
        Assert.Equal(HttpStatusCode.OK, getLoanResponse.StatusCode);
        var loan = await getLoanResponse.Content.ReadFromJsonAsync<LoanGetDto>();
        Assert.NotNull(loan);
        Assert.Null(loan.ReturnedAt);
        Assert.Equal(bookId, loan.BookId);
        Assert.Equal(userId, loan.UserId);

        // 7. PATCH api/loan/returnLoan/{LoanId} -> 204
        var returnResponse = await Client.PatchAsync($"api/loan/returnLoan/{loanId}", null);
        Assert.Equal(HttpStatusCode.NoContent, returnResponse.StatusCode);

        // 8. GET api/loan/{LoanId} -> 200, assert ReturnedAt is now set
        var getReturnedLoanResponse = await Client.GetAsync($"api/loan/{loanId}");
        Assert.Equal(HttpStatusCode.OK, getReturnedLoanResponse.StatusCode);
        var returnedLoan = await getReturnedLoanResponse.Content.ReadFromJsonAsync<LoanGetDto>();
        Assert.NotNull(returnedLoan);
        Assert.NotNull(returnedLoan.ReturnedAt);
    }

    [Fact]
    public async Task FullLifecycle_BookLoanedTwiceSequentially_BuildsCorrectLoanHistory()
    {
        //Arrange
        var authorResponse = await Client.PostAsJsonAsync("api/author", new AuthorCreateDto { Name = "J.K. Rowling" });
        var authorId = (await authorResponse.Content.ReadFromJsonAsync<CreatedIdResponse>())!.Id;

        var bookResponse = await Client.PostAsJsonAsync("api/books", new BookCreateDto
        {
            Title = "Harry Potter and the Philosopher's Stone",
            Description = "A young wizard story.",
            Isbn = "9780747532699",
            AuthorId = authorId,
            DatePublished = new DateOnly(1997, 6, 26),
            Rating = 4.9m
        });
        var bookId = (await bookResponse.Content.ReadFromJsonAsync<CreatedIdResponse>())!.Id;

        var userAResponse = await Client.PostAsJsonAsync("api/user", new UserCreateDto { Name = "User A" });
        var userAId = (await userAResponse.Content.ReadFromJsonAsync<CreatedIdResponse>())!.Id;

        var userBResponse = await Client.PostAsJsonAsync("api/user", new UserCreateDto { Name = "User B" });
        var userBId = (await userBResponse.Content.ReadFromJsonAsync<CreatedIdResponse>())!.Id;

        //Act
        // 1. Loan to User A and return it
        var loan1Response = await Client.PostAsJsonAsync("api/loan", new LoanCreateDto
        {
            BookId = bookId,
            UserId = userAId,
            DueAt = DateTime.UtcNow.AddDays(14)
        });
        Assert.Equal(HttpStatusCode.Created, loan1Response.StatusCode);
        var loan1Id = (await loan1Response.Content.ReadFromJsonAsync<CreatedIdResponse>())!.Id;
        var return1Response = await Client.PatchAsync($"api/loan/returnLoan/{loan1Id}", null);
        Assert.Equal(HttpStatusCode.NoContent, return1Response.StatusCode);

        // 2. Loan to User B and return it
        var loan2Response = await Client.PostAsJsonAsync("api/loan", new LoanCreateDto
        {
            BookId = bookId,
            UserId = userBId,
            DueAt = DateTime.UtcNow.AddDays(14)
        });
        Assert.Equal(HttpStatusCode.Created, loan2Response.StatusCode);
        var loan2Id = (await loan2Response.Content.ReadFromJsonAsync<CreatedIdResponse>())!.Id;
        var return2Response = await Client.PatchAsync($"api/loan/returnLoan/{loan2Id}", null);
        Assert.Equal(HttpStatusCode.NoContent, return2Response.StatusCode);

        //Assert
        var historyResponse = await Client.GetAsync($"api/loan?BookId={bookId}");
        Assert.Equal(HttpStatusCode.OK, historyResponse.StatusCode);
        var history = await historyResponse.Content.ReadFromJsonAsync<PagedResult<LoanGetDto>>();
        Assert.NotNull(history);
        Assert.Equal(2, history.TotalCount);
        Assert.All(history.Items, l => Assert.NotNull(l.ReturnedAt));
        Assert.Contains(history.Items, l => l.UserId == userAId);
        Assert.Contains(history.Items, l => l.UserId == userBId);
    }

    [Fact]
    public async Task FullLifecycle_SecondUserAttemptsToBorrowAlreadyLoanedBook_ReturnsConflictThenSucceedsAfterReturn()
    {
        //Arrange
        var authorResponse = await Client.PostAsJsonAsync("api/author", new AuthorCreateDto { Name = "Frank Herbert" });
        var authorId = (await authorResponse.Content.ReadFromJsonAsync<CreatedIdResponse>())!.Id;

        var bookResponse = await Client.PostAsJsonAsync("api/books", new BookCreateDto
        {
            Title = "Dune",
            Description = "Spice must flow.",
            Isbn = "9780441172719",
            AuthorId = authorId,
            DatePublished = new DateOnly(1965, 8, 1),
            Rating = 4.7m
        });
        var bookId = (await bookResponse.Content.ReadFromJsonAsync<CreatedIdResponse>())!.Id;

        var userAResponse = await Client.PostAsJsonAsync("api/user", new UserCreateDto { Name = "Paul Atreides" });
        var userAId = (await userAResponse.Content.ReadFromJsonAsync<CreatedIdResponse>())!.Id;

        var userBResponse = await Client.PostAsJsonAsync("api/user", new UserCreateDto { Name = "Feyd-Rautha" });
        var userBId = (await userBResponse.Content.ReadFromJsonAsync<CreatedIdResponse>())!.Id;

        //Act & Assert
        // User A borrows it -> 201
        var borrowAResponse = await Client.PostAsJsonAsync("api/loan", new LoanCreateDto
        {
            BookId = bookId,
            UserId = userAId,
            DueAt = DateTime.UtcNow.AddDays(14)
        });
        Assert.Equal(HttpStatusCode.Created, borrowAResponse.StatusCode);
        var loanId = (await borrowAResponse.Content.ReadFromJsonAsync<CreatedIdResponse>())!.Id;

        // User B attempts to borrow the same book -> 409
        var borrowBConflictResponse = await Client.PostAsJsonAsync("api/loan", new LoanCreateDto
        {
            BookId = bookId,
            UserId = userBId,
            DueAt = DateTime.UtcNow.AddDays(14)
        });
        Assert.Equal(HttpStatusCode.Conflict, borrowBConflictResponse.StatusCode);

        // User A returns it -> 204
        var returnResponse = await Client.PatchAsync($"api/loan/returnLoan/{loanId}", null);
        Assert.Equal(HttpStatusCode.NoContent, returnResponse.StatusCode);

        // User B tries again -> 201
        var borrowBSuccessResponse = await Client.PostAsJsonAsync("api/loan", new LoanCreateDto
        {
            BookId = bookId,
            UserId = userBId,
            DueAt = DateTime.UtcNow.AddDays(14)
        });
        Assert.Equal(HttpStatusCode.Created, borrowBSuccessResponse.StatusCode);
    }

    [Fact]
    public async Task FullLifecycle_DeletingAuthorWithPublishedBook_IsBlockedUntilBookIsDeleted()
    {
        //Arrange
        var authorResponse = await Client.PostAsJsonAsync("api/author", new AuthorCreateDto { Name = "C.S. Lewis" });
        var authorId = (await authorResponse.Content.ReadFromJsonAsync<CreatedIdResponse>())!.Id;

        var bookResponse = await Client.PostAsJsonAsync("api/books", new BookCreateDto
        {
            Title = "The Lion, the Witch and the Wardrobe",
            Description = "Narnia story.",
            Isbn = "9780064404990",
            AuthorId = authorId,
            DatePublished = new DateOnly(1950, 10, 16),
            Rating = 4.6m
        });
        var bookId = (await bookResponse.Content.ReadFromJsonAsync<CreatedIdResponse>())!.Id;

        //Act & Assert
        // DELETE author -> 409 Conflict because active book exists
        var deleteAuthorConflictResponse = await Client.DeleteAsync($"api/author/{authorId}");
        Assert.Equal(HttpStatusCode.Conflict, deleteAuthorConflictResponse.StatusCode);

        // DELETE book -> 204 NoContent
        var deleteBookResponse = await Client.DeleteAsync($"api/books/{bookId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteBookResponse.StatusCode);

        // DELETE author again -> 204 NoContent now that active book is gone
        var deleteAuthorSuccessResponse = await Client.DeleteAsync($"api/author/{authorId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteAuthorSuccessResponse.StatusCode);
    }

    [Fact]
    public async Task FullLifecycle_DeletingUserWithActiveLoan_IsBlockedUntilLoanIsReturned()
    {
        //Arrange
        var authorResponse = await Client.PostAsJsonAsync("api/author", new AuthorCreateDto { Name = "Brandon Sanderson" });
        var authorId = (await authorResponse.Content.ReadFromJsonAsync<CreatedIdResponse>())!.Id;

        var bookResponse = await Client.PostAsJsonAsync("api/books", new BookCreateDto
        {
            Title = "Mistborn: The Final Empire",
            Description = "Allomancy saga.",
            Isbn = "9780765311788",
            AuthorId = authorId,
            DatePublished = new DateOnly(2006, 7, 17),
            Rating = 4.9m
        });
        var bookId = (await bookResponse.Content.ReadFromJsonAsync<CreatedIdResponse>())!.Id;

        var userResponse = await Client.PostAsJsonAsync("api/user", new UserCreateDto { Name = "Vin" });
        var userId = (await userResponse.Content.ReadFromJsonAsync<CreatedIdResponse>())!.Id;

        var loanResponse = await Client.PostAsJsonAsync("api/loan", new LoanCreateDto
        {
            BookId = bookId,
            UserId = userId,
            DueAt = DateTime.UtcNow.AddDays(14)
        });
        var loanId = (await loanResponse.Content.ReadFromJsonAsync<CreatedIdResponse>())!.Id;

        //Act & Assert
        // DELETE user -> 409 Conflict because user has active unreturned loan
        var deleteUserConflictResponse = await Client.DeleteAsync($"api/user/{userId}");
        Assert.Equal(HttpStatusCode.Conflict, deleteUserConflictResponse.StatusCode);

        // Return loan -> 204 NoContent
        var returnResponse = await Client.PatchAsync($"api/loan/returnLoan/{loanId}", null);
        Assert.Equal(HttpStatusCode.NoContent, returnResponse.StatusCode);

        // DELETE user again -> 204 NoContent now that loan is returned
        var deleteUserSuccessResponse = await Client.DeleteAsync($"api/user/{userId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteUserSuccessResponse.StatusCode);
    }

    [Fact]
    public async Task FullLifecycle_BorrowingRequestChain_FailsCleanlyWhenBookDoesNotExist()
    {
        //Arrange
        var userResponse = await Client.PostAsJsonAsync("api/user", new UserCreateDto { Name = "Frodo Baggins" });
        var userId = (await userResponse.Content.ReadFromJsonAsync<CreatedIdResponse>())!.Id;
        var nonExistentBookId = Guid.CreateVersion7();

        var loanDto = new LoanCreateDto
        {
            BookId = nonExistentBookId,
            UserId = userId,
            DueAt = DateTime.UtcNow.AddDays(14)
        };

        //Act
        var response = await Client.PostAsJsonAsync("api/loan", loanDto);

        //Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task FullLifecycle_BorrowingRequestChain_FailsCleanlyWhenUserDoesNotExist()
    {
        //Arrange
        var authorResponse = await Client.PostAsJsonAsync("api/author", new AuthorCreateDto { Name = "Philip K. Dick" });
        var authorId = (await authorResponse.Content.ReadFromJsonAsync<CreatedIdResponse>())!.Id;

        var bookResponse = await Client.PostAsJsonAsync("api/books", new BookCreateDto
        {
            Title = "Do Androids Dream of Electric Sheep?",
            Description = "Cyberpunk classic.",
            Isbn = "9780345404474",
            AuthorId = authorId,
            DatePublished = new DateOnly(1968, 3, 1),
            Rating = 4.4m
        });
        var bookId = (await bookResponse.Content.ReadFromJsonAsync<CreatedIdResponse>())!.Id;
        var nonExistentUserId = Guid.CreateVersion7();

        var loanDto = new LoanCreateDto
        {
            BookId = bookId,
            UserId = nonExistentUserId,
            DueAt = DateTime.UtcNow.AddDays(14)
        };

        //Act
        var response = await Client.PostAsJsonAsync("api/loan", loanDto);

        //Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task FullLifecycle_ReturningAlreadyReturnedLoan_IsRejectedButDoesNotCorruptState()
    {
        //Arrange
        var authorResponse = await Client.PostAsJsonAsync("api/author", new AuthorCreateDto { Name = "Mary Shelley" });
        var authorId = (await authorResponse.Content.ReadFromJsonAsync<CreatedIdResponse>())!.Id;

        var bookResponse = await Client.PostAsJsonAsync("api/books", new BookCreateDto
        {
            Title = "Frankenstein",
            Description = "Gothic classic.",
            Isbn = "9780141439471",
            AuthorId = authorId,
            DatePublished = new DateOnly(1818, 1, 1),
            Rating = 4.3m
        });
        var bookId = (await bookResponse.Content.ReadFromJsonAsync<CreatedIdResponse>())!.Id;

        var userResponse = await Client.PostAsJsonAsync("api/user", new UserCreateDto { Name = "Victor" });
        var userId = (await userResponse.Content.ReadFromJsonAsync<CreatedIdResponse>())!.Id;

        var loanResponse = await Client.PostAsJsonAsync("api/loan", new LoanCreateDto
        {
            BookId = bookId,
            UserId = userId,
            DueAt = DateTime.UtcNow.AddDays(14)
        });
        var loanId = (await loanResponse.Content.ReadFromJsonAsync<CreatedIdResponse>())!.Id;

        var returnResponse = await Client.PatchAsync($"api/loan/returnLoan/{loanId}", null);
        Assert.Equal(HttpStatusCode.NoContent, returnResponse.StatusCode);

        var firstGetLoanResponse = await Client.GetAsync($"api/loan/{loanId}");
        var firstReturnedLoan = await firstGetLoanResponse.Content.ReadFromJsonAsync<LoanGetDto>();
        Assert.NotNull(firstReturnedLoan);
        var initialReturnedAt = firstReturnedLoan.ReturnedAt;
        Assert.NotNull(initialReturnedAt);

        //Act
        // PATCH return again on the same LoanId -> 400
        var secondReturnResponse = await Client.PatchAsync($"api/loan/returnLoan/{loanId}", null);

        //Assert
        Assert.Equal(HttpStatusCode.BadRequest, secondReturnResponse.StatusCode);

        var secondGetLoanResponse = await Client.GetAsync($"api/loan/{loanId}");
        Assert.Equal(HttpStatusCode.OK, secondGetLoanResponse.StatusCode);
        var secondReturnedLoan = await secondGetLoanResponse.Content.ReadFromJsonAsync<LoanGetDto>();
        Assert.NotNull(secondReturnedLoan);
        Assert.Equal(initialReturnedAt, secondReturnedLoan.ReturnedAt);
    }
}
