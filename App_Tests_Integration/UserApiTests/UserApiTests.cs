using System.Net;
using System.Net.Http.Json;
using App_BLL.Common.Paging;
using App_BLL.Dtos.UsersDtos;
using App_DAL.Database;
using App_Tests_Integration.Helper.Seeders;
using App_Tests_Integration.Infrastructre;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace App_Tests_Integration.UserApiTests;

public class UserApiTests : BaseIntegrationTest
{
    public UserApiTests(ApiWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetAllUsersAsync_ReturnsOkStatus()
    {
        //Arrange

        //Act
        var response = await Client.GetAsync("api/user");

        //Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetAllUsersAsync_ReturnsCorrectTotalCount()
    {
        //Arrange
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var seeder = new UserSeeder(db);
        int count = 5;
        await seeder.SeedManyAsync(count);

        //Act
        var response = await Client.GetAsync("api/user");

        //Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var pagedResult = await response.Content.ReadFromJsonAsync<PagedResult<UserGetDto>>();
        Assert.NotNull(pagedResult);
        Assert.Equal(count, pagedResult.TotalCount);
    }

    [Fact]
    public async Task GetAllUsersAsync_FilterByName_ReturnsOnlyMatchingUsers()
    {
        //Arrange
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var seeder = new UserSeeder(db);
        await seeder.SeedOneAsync(o => o.Name = "Alice Smith");
        await seeder.SeedOneAsync(o => o.Name = "Alice Cooper");
        await seeder.SeedOneAsync(o => o.Name = "Bob Jones");

        //Act
        var response = await Client.GetAsync("api/user?Name=Alice");

        //Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var pagedResult = await response.Content.ReadFromJsonAsync<PagedResult<UserGetDto>>();
        Assert.NotNull(pagedResult);
        Assert.Equal(2, pagedResult.TotalCount);
        Assert.All(pagedResult.Items, u => Assert.Contains("Alice", u.Name));
    }

    [Fact]
    public async Task GetUserByIdAsync_SendExistingId_ReturnsOkStatusAndCorrectUser()
    {
        //Arrange
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var seeder = new UserSeeder(db);
        var user = await seeder.SeedOneAsync();

        //Act
        var response = await Client.GetAsync($"api/user/{user.Id}");

        //Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var returnedUser = await response.Content.ReadFromJsonAsync<UserGetDto>();
        Assert.NotNull(returnedUser);
        Assert.Equal(user.Id, returnedUser.Id);
    }

    [Fact]
    public async Task GetUserByIdAsync_SendNonExistingId_ReturnsNotFoundStatus()
    {
        //Arrange
        var id = Guid.CreateVersion7();

        //Act
        var response = await Client.GetAsync($"api/user/{id}");

        //Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateUserAsync_ValidPayload_ReturnsCreatedAndPersistsUser()
    {
        //Arrange
        var dto = new UserCreateDto { Name = "Jane Doe" };

        //Act
        var response = await Client.PostAsJsonAsync("api/user", dto);

        //Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var persisted = await db.Users.FirstOrDefaultAsync(u => u.Name == dto.Name);
        Assert.NotNull(persisted);
    }

    [Fact]
    public async Task CreateUserAsync_MissingName_ReturnsBadRequest()
    {
        //Arrange
        var dto = new UserCreateDto { Name = string.Empty };

        //Act
        var response = await Client.PostAsJsonAsync("api/user", dto);

        //Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateUserAsync_NameExceedsMaxLength_ReturnsBadRequest()
    {
        //Arrange
        var dto = new UserCreateDto { Name = new string('A', 65) };

        //Act
        var response = await Client.PostAsJsonAsync("api/user", dto);

        //Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateUserAsync_ExistingUser_ReturnsNoContentAndPersistsChange()
    {
        //Arrange
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var seeder = new UserSeeder(db);
        var user = await seeder.SeedOneAsync(o => o.Name = "Original Name");
        var dto = new UserEditDto { Name = "Updated Name" };

        //Act
        var response = await Client.PutAsJsonAsync($"api/user/{user.Id}", dto);

        //Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        using var assertScope = Factory.Services.CreateScope();
        var assertDb = assertScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var updated = await assertDb.Users.FindAsync(user.Id);
        Assert.NotNull(updated);
        Assert.Equal("Updated Name", updated.Name);
    }

    [Fact]
    public async Task UpdateUserAsync_NonExistingId_ReturnsNotFound()
    {
        //Arrange
        var id = Guid.CreateVersion7();
        var dto = new UserEditDto { Name = "Updated Name" };

        //Act
        var response = await Client.PutAsJsonAsync($"api/user/{id}", dto);

        //Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateUserAsync_DeletedUser_ReturnsNotFound()
    {
        //Arrange
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var seeder = new UserSeeder(db);
        var user = await seeder.SeedOneAsync(o => o.IsDeleted = true);
        var dto = new UserEditDto { Name = "Updated Name" };

        //Act
        var response = await Client.PutAsJsonAsync($"api/user/{user.Id}", dto);

        //Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteUserAsync_ExistingUserWithNoLoans_ReturnsNoContent()
    {
        //Arrange
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var seeder = new UserSeeder(db);
        var user = await seeder.SeedOneAsync();

        //Act
        var response = await Client.DeleteAsync($"api/user/{user.Id}");

        //Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task DeleteUserAsync_NonExistingId_ReturnsNotFound()
    {
        //Arrange
        var id = Guid.CreateVersion7();

        //Act
        var response = await Client.DeleteAsync($"api/user/{id}");

        //Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteUserAsync_AlreadyDeletedUser_ReturnsNotFound()
    {
        //Arrange
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var seeder = new UserSeeder(db);
        var user = await seeder.SeedOneAsync(o => o.IsDeleted = true);

        //Act
        var response = await Client.DeleteAsync($"api/user/{user.Id}");

        //Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteUserAsync_UserHasActiveLoan_ReturnsConflict()
    {
        //Arrange
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userSeeder = new UserSeeder(db);
        var loanSeeder = new LoanSeeder(db);
        var user = await userSeeder.SeedOneAsync();
        await loanSeeder.SeedOneAsync(o => o.User = user);

        //Act
        var response = await Client.DeleteAsync($"api/user/{user.Id}");

        //Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }
}