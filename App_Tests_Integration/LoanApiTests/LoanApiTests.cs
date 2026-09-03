using System.Net;
using System.Net.Http.Json;
using App_BLL.Common.Paging;
using App_BLL.Dtos.LoansDtos;
using App_DAL.Database;
using App_Tests_Integration.Helper.Seeders;
using App_Tests_Integration.Infrastructre;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace App_Tests_Integration.LoanApiTests;

public class LoanApiTests : BaseIntegrationTest
{
    public LoanApiTests(ApiWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetLoansAsync_ReturnsOkStatus()
    {
        //Arrange

        //Act
        var response = await Client.GetAsync("api/loan");

        //Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetLoansAsync_ReturnsCorrectTotalCount()
    {
        //Arrange
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var seeder = new LoanSeeder(db);
        int count = 5;
        await seeder.SeedManyAsync(count);

        //Act
        var response = await Client.GetAsync("api/loan");

        //Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var pagedResult = await response.Content.ReadFromJsonAsync<PagedResult<LoanGetDto>>();
        Assert.NotNull(pagedResult);
        Assert.Equal(count, pagedResult.TotalCount);
    }

    [Fact]
    public async Task GetLoansAsync_FilterByBookId_ReturnsOnlyThatBooksLoans()
    {
        //Arrange
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var seeder = new LoanSeeder(db);
        var loans = await seeder.SeedManyAsync(3);
        var targetBookId = loans[0].BookId;

        //Act
        var response = await Client.GetAsync($"api/loan?BookId={targetBookId}");

        //Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var pagedResult = await response.Content.ReadFromJsonAsync<PagedResult<LoanGetDto>>();
        Assert.NotNull(pagedResult);
        Assert.Equal(1, pagedResult.TotalCount);
        Assert.All(pagedResult.Items, l => Assert.Equal(targetBookId, l.BookId));
    }

    [Fact]
    public async Task GetLoansAsync_FilterByUserId_ReturnsOnlyThatUsersLoans()
    {
        //Arrange
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userSeeder = new UserSeeder(db);
        var loanSeeder = new LoanSeeder(db);
        var targetUser = await userSeeder.SeedOneAsync(o => o.Name = "Target User");
        var otherUser = await userSeeder.SeedOneAsync(o => o.Name = "Other User");
        await loanSeeder.SeedOneAsync(o => o.User = targetUser);
        await loanSeeder.SeedOneAsync(o => o.User = otherUser);

        //Act
        var response = await Client.GetAsync($"api/loan?UserId={targetUser.Id}");

        //Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var pagedResult = await response.Content.ReadFromJsonAsync<PagedResult<LoanGetDto>>();
        Assert.NotNull(pagedResult);
        Assert.Equal(1, pagedResult.TotalCount);
        Assert.All(pagedResult.Items, l => Assert.Equal(targetUser.Id, l.UserId));
    }

    [Fact]
    public async Task GetLoansAsync_FilterByIsReturnedTrue_ReturnsOnlyReturnedLoans()
    {
        //Arrange
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var seeder = new LoanSeeder(db);
        await seeder.SeedOneAsync(o => o.MarkAsReturned = true);
        await seeder.SeedOneAsync(o => o.MarkAsReturned = false);

        //Act
        var response = await Client.GetAsync("api/loan?IsReturned=true");

        //Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var pagedResult = await response.Content.ReadFromJsonAsync<PagedResult<LoanGetDto>>();
        Assert.NotNull(pagedResult);
        Assert.Equal(1, pagedResult.TotalCount);
        Assert.All(pagedResult.Items, l => Assert.NotNull(l.ReturnedAt));
    }

    [Fact]
    public async Task GetLoansAsync_FilterByIsReturnedFalse_ReturnsOnlyActiveLoans()
    {
        //Arrange
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var seeder = new LoanSeeder(db);
        await seeder.SeedOneAsync(o => o.MarkAsReturned = true);
        await seeder.SeedOneAsync(o => o.MarkAsReturned = false);

        //Act
        var response = await Client.GetAsync("api/loan?IsReturned=false");

        //Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var pagedResult = await response.Content.ReadFromJsonAsync<PagedResult<LoanGetDto>>();
        Assert.NotNull(pagedResult);
        Assert.Equal(1, pagedResult.TotalCount);
        Assert.All(pagedResult.Items, l => Assert.Null(l.ReturnedAt));
    }

    [Fact]
    public async Task GetLoanByIdAsync_SendExistingId_ReturnsOkStatusAndCorrectLoan()
    {
        //Arrange
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var seeder = new LoanSeeder(db);
        var loan = await seeder.SeedOneAsync();

        //Act
        var response = await Client.GetAsync($"api/loan/{loan.Id}");

        //Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var returnedLoan = await response.Content.ReadFromJsonAsync<LoanGetDto>();
        Assert.NotNull(returnedLoan);
        Assert.Equal(loan.Id, returnedLoan.Id);
    }

    [Fact]
    public async Task GetLoanByIdAsync_SendNonExistingId_ReturnsNotFoundStatus()
    {
        //Arrange
        var id = Guid.CreateVersion7();

        //Act
        var response = await Client.GetAsync($"api/loan/{id}");

        //Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task LoanBookAsync_ValidPayload_ReturnsCreatedAndPersistsLoan()
    {
        //Arrange
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var book = await new BookSeeder(db).SeedOneAsync();
        var user = await new UserSeeder(db).SeedOneAsync();
        var dto = new LoanCreateDto
        {
            BookId = book.Id,
            UserId = user.Id,
            DueAt = DateTime.UtcNow.AddDays(14)
        };

        //Act
        var response = await Client.PostAsJsonAsync("api/loan", dto);

        //Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var assertScope = Factory.Services.CreateScope();
        var assertDb = assertScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var persisted = await assertDb.Loans.FirstOrDefaultAsync(l => l.BookId == book.Id);
        Assert.NotNull(persisted);
        Assert.Null(persisted.ReturnedAt);
    }

    [Fact]
    public async Task LoanBookAsync_NonExistingBook_ReturnsNotFound()
    {
        //Arrange
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = await new UserSeeder(db).SeedOneAsync();
        var dto = new LoanCreateDto
        {
            BookId = Guid.CreateVersion7(),
            UserId = user.Id,
            DueAt = DateTime.UtcNow.AddDays(14)
        };

        //Act
        var response = await Client.PostAsJsonAsync("api/loan", dto);

        //Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task LoanBookAsync_NonExistingUser_ReturnsNotFound()
    {
        //Arrange
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var book = await new BookSeeder(db).SeedOneAsync();
        var dto = new LoanCreateDto
        {
            BookId = book.Id,
            UserId = Guid.CreateVersion7(),
            DueAt = DateTime.UtcNow.AddDays(14)
        };

        //Act
        var response = await Client.PostAsJsonAsync("api/loan", dto);

        //Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task LoanBookAsync_DueDateInThePast_ReturnsBadRequest()
    {
        //Arrange
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var book = await new BookSeeder(db).SeedOneAsync();
        var user = await new UserSeeder(db).SeedOneAsync();
        var dto = new LoanCreateDto
        {
            BookId = book.Id,
            UserId = user.Id,
            DueAt = DateTime.UtcNow.AddDays(-1)
        };

        //Act
        var response = await Client.PostAsJsonAsync("api/loan", dto);

        //Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task LoanBookAsync_BookAlreadyActivelyLoaned_ReturnsConflict()
    {
        //Arrange
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var book = await new BookSeeder(db).SeedOneAsync();
        var user1 = await new UserSeeder(db).SeedOneAsync();
        var user2 = await new UserSeeder(db).SeedOneAsync(o => o.Name = "Second User");
        var loanSeeder = new LoanSeeder(db);
        await loanSeeder.SeedOneAsync(o =>
        {
            o.Book = book;
            o.User = user1;
            o.MarkAsReturned = false;
        });
        var dto = new LoanCreateDto
        {
            BookId = book.Id,
            UserId = user2.Id,
            DueAt = DateTime.UtcNow.AddDays(14)
        };

        //Act
        var response = await Client.PostAsJsonAsync("api/loan", dto);

        //Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task LoanBookAsync_ConcurrentRequestsForSameBook_OnlyOneSucceeds()
    {
        //Arrange
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var book = await new BookSeeder(db).SeedOneAsync();
        var user1 = await new UserSeeder(db).SeedOneAsync(o => o.Name = "User One");
        var user2 = await new UserSeeder(db).SeedOneAsync(o => o.Name = "User Two");

        var dto1 = new LoanCreateDto
        {
            BookId = book.Id,
            UserId = user1.Id,
            DueAt = DateTime.UtcNow.AddDays(14)
        };
        var dto2 = new LoanCreateDto
        {
            BookId = book.Id,
            UserId = user2.Id,
            DueAt = DateTime.UtcNow.AddDays(14)
        };

        //Act
        var task1 = Client.PostAsJsonAsync("api/loan", dto1);
        var task2 = Client.PostAsJsonAsync("api/loan", dto2);
        var responses = await Task.WhenAll(task1, task2);

        //Assert
        Assert.Contains(responses, r => r.StatusCode == HttpStatusCode.Created);
        Assert.Contains(responses, r => r.StatusCode == HttpStatusCode.Conflict);

        using var assertScope = Factory.Services.CreateScope();
        var assertDb = assertScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var activeLoansCount = await assertDb.Loans.CountAsync(l => l.BookId == book.Id && l.ReturnedAt == null);
        Assert.Equal(1, activeLoansCount);
    }

    [Fact]
    public async Task ReturnBookAsync_ExistingActiveLoan_ReturnsNoContentAndSetsReturnedAt()
    {
        //Arrange
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var loan = await new LoanSeeder(db).SeedOneAsync(o => o.MarkAsReturned = false);

        //Act
        var response = await Client.PatchAsync($"api/loan/returnLoan/{loan.Id}", null);

        //Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        using var assertScope = Factory.Services.CreateScope();
        var assertDb = assertScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var returnedLoan = await assertDb.Loans.FindAsync(loan.Id);
        Assert.NotNull(returnedLoan);
        Assert.NotNull(returnedLoan.ReturnedAt);
    }

    [Fact]
    public async Task ReturnBookAsync_NonExistingLoan_ReturnsNotFound()
    {
        //Arrange
        var id = Guid.CreateVersion7();

        //Act
        var response = await Client.PatchAsync($"api/loan/returnLoan/{id}", null);

        //Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ReturnBookAsync_AlreadyReturnedLoan_ReturnsBadRequest()
    {
        //Arrange
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var loan = await new LoanSeeder(db).SeedOneAsync(o => o.MarkAsReturned = true);

        //Act
        var response = await Client.PatchAsync($"api/loan/returnLoan/{loan.Id}", null);

        //Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ReturnBookAsync_AfterReturn_BookCanBeLoanedAgain()
    {
        //Arrange
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var loan = await new LoanSeeder(db).SeedOneAsync(o => o.MarkAsReturned = false);
        var newUser = await new UserSeeder(db).SeedOneAsync(o => o.Name = "Another User");
        var returnResponse = await Client.PatchAsync($"api/loan/returnLoan/{loan.Id}", null);
        Assert.Equal(HttpStatusCode.NoContent, returnResponse.StatusCode);

        var newLoanDto = new LoanCreateDto
        {
            BookId = loan.BookId,
            UserId = newUser.Id,
            DueAt = DateTime.UtcNow.AddDays(14)
        };

        //Act
        var response = await Client.PostAsJsonAsync("api/loan", newLoanDto);

        //Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }
}