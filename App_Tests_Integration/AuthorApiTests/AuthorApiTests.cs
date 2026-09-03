using System.Net;
using System.Net.Http.Json;
using App_BLL.Common.Paging;
using App_BLL.Dtos.AuthorsDtos;
using App_DAL.Database;
using App_Tests_Integration.Helper.Seeders;
using App_Tests_Integration.Infrastructre;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace App_Tests_Integration.AuthorApiTests;

public class AuthorApiTests : BaseIntegrationTest
{
    public AuthorApiTests(ApiWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetAllAuthorsAsync_ReturnsOkStatus()
    {
        //Arrange

        //Act
        var response = await Client.GetAsync("api/author");

        //Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetAllAuthorsAsync_ReturnsCorrectTotalCount()
    {
        //Arrange
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var seeder = new AuthorSeeder(db);
        int count = 5;
        await seeder.SeedManyAsync(count);

        //Act
        var response = await Client.GetAsync("api/author");

        //Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var pagedResult = await response.Content.ReadFromJsonAsync<PagedResult<AuthorGetDto>>();
        Assert.NotNull(pagedResult);
        Assert.Equal(count, pagedResult.TotalCount);
    }

    [Fact]
    public async Task GetAllAuthorsAsync_FilterByName_ReturnsOnlyMatchingAuthors()
    {
        //Arrange
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var seeder = new AuthorSeeder(db);
        await seeder.SeedOneAsync(o => o.Name = "George Orwell");
        await seeder.SeedOneAsync(o => o.Name = "George R.R. Martin");
        await seeder.SeedOneAsync(o => o.Name = "Aldous Huxley");

        //Act
        var response = await Client.GetAsync("api/author?Name=George");

        //Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var pagedResult = await response.Content.ReadFromJsonAsync<PagedResult<AuthorGetDto>>();
        Assert.NotNull(pagedResult);
        Assert.Equal(2, pagedResult.TotalCount);
        Assert.All(pagedResult.Items, a => Assert.Contains("George", a.Name));
    }

    [Fact]
    public async Task GetAuthorByIdAsync_SendExistingId_ReturnsOkStatusAndCorrectAuthor()
    {
        //Arrange
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var seeder = new AuthorSeeder(db);
        var author = await seeder.SeedOneAsync();

        //Act
        var response = await Client.GetAsync($"api/author/{author.Id}");

        //Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var returnedAuthor = await response.Content.ReadFromJsonAsync<AuthorGetDto>();
        Assert.NotNull(returnedAuthor);
        Assert.Equal(author.Id, returnedAuthor.Id);
    }

    [Fact]
    public async Task GetAuthorByIdAsync_SendNonExistingId_ReturnsNotFoundStatus()
    {
        //Arrange
        var id = Guid.CreateVersion7();

        //Act
        var response = await Client.GetAsync($"api/author/{id}");

        //Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateAuthorAsync_ValidPayload_ReturnsCreatedAndPersistsAuthor()
    {
        //Arrange
        var dto = new AuthorCreateDto { Name = "Arthur Conan Doyle" };

        //Act
        var response = await Client.PostAsJsonAsync("api/author", dto);

        //Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var persisted = await db.Authors.FirstOrDefaultAsync(a => a.Name == dto.Name);
        Assert.NotNull(persisted);
    }

    [Fact]
    public async Task CreateAuthorAsync_DuplicateName_ReturnsCreated()
    {
        //Arrange
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var seeder = new AuthorSeeder(db);
        await seeder.SeedOneAsync(o => o.Name = "Duplicate Author");
        var dto = new AuthorCreateDto { Name = "Duplicate Author" };

        //Act
        var response = await Client.PostAsJsonAsync("api/author", dto);

        //Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateAuthorAsync_MissingName_ReturnsBadRequest()
    {
        //Arrange
        var dto = new AuthorCreateDto { Name = string.Empty };

        //Act
        var response = await Client.PostAsJsonAsync("api/author", dto);

        //Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateAuthorAsync_NameExceedsMaxLength_ReturnsBadRequest()
    {
        //Arrange
        var dto = new AuthorCreateDto { Name = new string('A', 65) };

        //Act
        var response = await Client.PostAsJsonAsync("api/author", dto);

        //Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateAuthorAsync_ExistingAuthor_ReturnsNoContentAndPersistsChange()
    {
        //Arrange
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var seeder = new AuthorSeeder(db);
        var author = await seeder.SeedOneAsync(o => o.Name = "Original Name");
        var dto = new AuthorEditDto { Name = "Updated Name" };

        //Act
        var response = await Client.PutAsJsonAsync($"api/author/{author.Id}", dto);

        //Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        using var assertScope = Factory.Services.CreateScope();
        var assertDb = assertScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var updated = await assertDb.Authors.FindAsync(author.Id);
        Assert.NotNull(updated);
        Assert.Equal("Updated Name", updated.Name);
    }

    [Fact]
    public async Task UpdateAuthorAsync_NonExistingId_ReturnsNotFound()
    {
        //Arrange
        var id = Guid.CreateVersion7();
        var dto = new AuthorEditDto { Name = "Updated Name" };

        //Act
        var response = await Client.PutAsJsonAsync($"api/author/{id}", dto);

        //Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateAuthorAsync_DeletedAuthor_ReturnsNotFound()
    {
        //Arrange
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var seeder = new AuthorSeeder(db);
        var author = await seeder.SeedOneAsync(o => o.IsDeleted = true);
        var dto = new AuthorEditDto { Name = "Updated Name" };

        //Act
        var response = await Client.PutAsJsonAsync($"api/author/{author.Id}", dto);

        //Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteAuthorAsync_ExistingAuthorWithNoBooks_ReturnsNoContent()
    {
        //Arrange
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var seeder = new AuthorSeeder(db);
        var author = await seeder.SeedOneAsync();

        //Act
        var response = await Client.DeleteAsync($"api/author/{author.Id}");

        //Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task DeleteAuthorAsync_NonExistingId_ReturnsNotFound()
    {
        //Arrange
        var id = Guid.CreateVersion7();

        //Act
        var response = await Client.DeleteAsync($"api/author/{id}");

        //Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteAuthorAsync_AlreadyDeletedAuthor_ReturnsNotFound()
    {
        //Arrange
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var seeder = new AuthorSeeder(db);
        var author = await seeder.SeedOneAsync(o => o.IsDeleted = true);

        //Act
        var response = await Client.DeleteAsync($"api/author/{author.Id}");

        //Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteAuthorAsync_AuthorHasActiveBook_ReturnsConflict()
    {
        //Arrange
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var authorSeeder = new AuthorSeeder(db);
        var bookSeeder = new BookSeeder(db);
        var author = await authorSeeder.SeedOneAsync();
        await bookSeeder.SeedOneAsync(o => o.Author = author);

        //Act
        var response = await Client.DeleteAsync($"api/author/{author.Id}");

        //Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }
}