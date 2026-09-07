using System.Net;
using System.Net.Http.Json;
using App_BLL.Common.Paging;
using App_BLL.Dtos.BooksDtos;
using App_DAL.Database;
using App_Tests_Integration.Helper.Seeders;
using App_Tests_Integration.Infrastructre;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace App_Tests_Integration.BookApiTests;

public class BookApiTests : BaseIntegrationTest
{
    public BookApiTests(ApiWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetAllBooksAsync_ReturnsOkStatus()
    {
        //Arrange
        
        //Act
        var response = await Client.GetAsync("api/books");

        //Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
    
    [Fact]
    public async Task GetAllBooksAsync_ReturnsCorrectTotalCount()
    {
        //Arrange
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var seeder = new BookSeeder(db);
        int count = 10;
        var books = await seeder.SeedManyAsync(count);

        //Act
        var response = await Client.GetAsync("api/books");

        //Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var pagedResult = await response.Content.ReadFromJsonAsync<PagedResult<BookGetDto>>();
        Assert.NotNull(pagedResult);
        Assert.Equal(count, pagedResult.TotalCount);
    }

    [Fact]
    public async Task GetAllBooksAsync_FilterByTitle_ReturnsOnlyMatchingBooks()
    {
        //Arrange
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var seeder = new BookSeeder(db);
        await seeder.SeedOneAsync(o => o.Title = "The Fellowship of the Ring");
        await seeder.SeedOneAsync(o => o.Title = "The Two Towers");
        await seeder.SeedOneAsync(o => o.Title = "Brave New World");

        //Act
        var response = await Client.GetAsync("api/books?Title=The");

        //Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var pagedResult = await response.Content.ReadFromJsonAsync<PagedResult<BookGetDto>>();
        Assert.NotNull(pagedResult);
        Assert.Equal(2, pagedResult.TotalCount);
        Assert.All(pagedResult.Items, b => Assert.Contains("The", b.Title));
    }

    [Fact]
    public async Task GetAllBooksAsync_FilterByIsbn_ReturnsOnlyMatchingBook()
    {
        //Arrange
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var seeder = new BookSeeder(db);
        var book = await seeder.SeedOneAsync(o => o.Isbn = "9780000000001");
        await seeder.SeedOneAsync(o => o.Isbn = "9780000000002");

        //Act
        var response = await Client.GetAsync($"api/books?Isbn={book.Isbn}");

        //Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var pagedResult = await response.Content.ReadFromJsonAsync<PagedResult<BookGetDto>>();
        Assert.NotNull(pagedResult);
        Assert.Equal(1, pagedResult.TotalCount);
        Assert.Equal(book.Isbn, pagedResult.Items[0].Isbn);
    }

    [Fact]
    public async Task GetAllBooksAsync_FilterByAuthorId_ReturnsOnlyThatAuthorsBooks()
    {
        //Arrange
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var authorSeeder = new AuthorSeeder(db);
        var bookSeeder = new BookSeeder(db);
        var author1 = await authorSeeder.SeedOneAsync(o => o.Name = "Author One");
        var author2 = await authorSeeder.SeedOneAsync(o => o.Name = "Author Two");
        await bookSeeder.SeedOneAsync(o => o.Author = author1);
        await bookSeeder.SeedOneAsync(o => o.Author = author2);

        //Act
        var response = await Client.GetAsync($"api/books?AuthorId={author1.Id}");

        //Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var pagedResult = await response.Content.ReadFromJsonAsync<PagedResult<BookGetDto>>();
        Assert.NotNull(pagedResult);
        Assert.Equal(1, pagedResult.TotalCount);
        Assert.Equal(author1.Id, pagedResult.Items[0].AuthorId);
    }

    [Fact]
    public async Task GetAllBooksAsync_FilterByAuthorName_ReturnsOnlyMatchingBooks()
    {
        //Arrange
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var authorSeeder = new AuthorSeeder(db);
        var bookSeeder = new BookSeeder(db);
        var author1 = await authorSeeder.SeedOneAsync(o => o.Name = "George Orwell");
        var author2 = await authorSeeder.SeedOneAsync(o => o.Name = "Isaac Asimov");
        await bookSeeder.SeedOneAsync(o => o.Author = author1);
        await bookSeeder.SeedOneAsync(o => o.Author = author2);

        //Act
        var response = await Client.GetAsync("api/books?AuthorName=Orwell");

        //Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var pagedResult = await response.Content.ReadFromJsonAsync<PagedResult<BookGetDto>>();
        Assert.NotNull(pagedResult);
        Assert.Equal(1, pagedResult.TotalCount);
        Assert.Equal(author1.Id, pagedResult.Items[0].AuthorId);
    }

    [Fact]
    public async Task GetAllBooksAsync_FilterByRatingRange_ReturnsOnlyBooksWithinRange()
    {
        //Arrange
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var seeder = new BookSeeder(db);
        await seeder.SeedOneAsync(o => o.Rating = 1.5m);
        await seeder.SeedOneAsync(o => o.Rating = 3.5m);
        await seeder.SeedOneAsync(o => o.Rating = 4.8m);

        //Act
        var response = await Client.GetAsync("api/books?MinRating=3.0&MaxRating=4.0");

        //Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var pagedResult = await response.Content.ReadFromJsonAsync<PagedResult<BookGetDto>>();
        Assert.NotNull(pagedResult);
        Assert.Equal(1, pagedResult.TotalCount);
        Assert.Equal(3.5m, pagedResult.Items[0].Rating);
    }

    [Fact]
    public async Task GetAllBooksAsync_FilterByDatePublishedRange_ReturnsOnlyBooksWithinRange()
    {
        //Arrange
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var seeder = new BookSeeder(db);
        await seeder.SeedOneAsync(o => o.DatePublished = new DateOnly(2020, 1, 1));
        await seeder.SeedOneAsync(o => o.DatePublished = new DateOnly(2022, 6, 15));
        await seeder.SeedOneAsync(o => o.DatePublished = new DateOnly(2024, 12, 31));

        //Act
        var response = await Client.GetAsync("api/books?StartDatePublished=2021-01-01&EndDatePublished=2023-01-01");

        //Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var pagedResult = await response.Content.ReadFromJsonAsync<PagedResult<BookGetDto>>();
        Assert.NotNull(pagedResult);
        Assert.Equal(1, pagedResult.TotalCount);
        Assert.Equal(new DateOnly(2022, 6, 15), pagedResult.Items[0].DatePublished);
    }

    [Fact]
    public async Task GetAllBooksAsync_PageBeyondAvailableData_ReturnsEmptyItemsWithCorrectTotalCount()
    {
        //Arrange
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var seeder = new BookSeeder(db);
        await seeder.SeedManyAsync(3);

        //Act
        var response = await Client.GetAsync("api/books?PageNumber=5000");

        //Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var pagedResult = await response.Content.ReadFromJsonAsync<PagedResult<BookGetDto>>();
        Assert.NotNull(pagedResult);
        Assert.Equal(3, pagedResult.TotalCount);
        Assert.Empty(pagedResult.Items);
    }

    [Fact]
    public async Task GetBookByIdAsync_SendExistingId_ReturnsOkStatusAndCorrectBook()
    {
        //Arrange
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var seeder = new BookSeeder(db);
        var book = await seeder.SeedOneAsync();

        //Act
        var response = await Client.GetAsync($"api/books/{book.Id}");

        //Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var returnedBook = await response.Content.ReadFromJsonAsync<BookGetDto>();
        Assert.NotNull(returnedBook);
        Assert.Equal(book.Id, returnedBook.Id);
    }

    [Fact]
    public async Task GetBookByIdAsync_SendExistingId_ReturnsCorrectAuthorName()
    {
        //Arrange
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var author = await new AuthorSeeder(db).SeedOneAsync(o => o.Name = "J.R.R. Tolkien");
        var book = await new BookSeeder(db).SeedOneAsync(o => o.Author = author);

        //Act
        var response = await Client.GetAsync($"api/books/{book.Id}");

        //Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var returnedBook = await response.Content.ReadFromJsonAsync<BookGetDto>();
        Assert.NotNull(returnedBook);
        Assert.Equal("J.R.R. Tolkien", returnedBook.AuthorName);
    }
    
    [Fact]
    public async Task GetBookByIdAsync_SendNonExistingId_ReturnsNotFoundStatusAndNoBook()
    {
        //Arrange
        var id = Guid.CreateVersion7();
        
        //Act
        var response = await Client.GetAsync($"api/books/{id}");

        //Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetBookByIsbnAsync_SendExistingIsbn_ReturnsOkStatusAndCorrectBook()
    {
        //Arrange
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var seeder = new BookSeeder(db);
        var book = await seeder.SeedOneAsync();

        //Act
        var response = await Client.GetAsync($"api/books/{book.Isbn}");

        //Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var returnedBook = await response.Content.ReadFromJsonAsync<BookGetDto>();
        Assert.NotNull(returnedBook);
        Assert.Equal(book.Isbn, returnedBook.Isbn);
    }

    [Fact]
    public async Task GetBookByIsbnAsync_SendNonExistingIsbn_ReturnsNotFoundStatus()
    {
        //Arrange
        var isbn = "9789999999999";

        //Act
        var response = await Client.GetAsync($"api/books/{isbn}");

        //Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateBookAsync_ValidPayload_ReturnsCreatedAndPersistsBook()
    {
        //Arrange
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var author = await new AuthorSeeder(db).SeedOneAsync();
        var dto = new BookCreateDto
        {
            Title = "Clean Code",
            Description = "A handbook of agile software craftsmanship.",
            Isbn = "9780132350884",
            AuthorId = author.Id,
            DatePublished = new DateOnly(2008, 8, 1),
            Rating = 4.5m
        };

        //Act
        var response = await Client.PostAsJsonAsync("api/books", dto);

        //Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var assertScope = Factory.Services.CreateScope();
        var assertDb = assertScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var persisted = await assertDb.Books.FirstOrDefaultAsync(b => b.Isbn == dto.Isbn);
        Assert.NotNull(persisted);
    }

    [Fact]
    public async Task CreateBookAsync_NonExistingAuthorId_ReturnsNotFound()
    {
        //Arrange
        var dto = new BookCreateDto
        {
            Title = "Some Book",
            Description = "Some description.",
            Isbn = "9781234567890",
            AuthorId = Guid.CreateVersion7(),
            DatePublished = DateOnly.FromDateTime(DateTime.UtcNow),
            Rating = 4.0m
        };

        //Act
        var response = await Client.PostAsJsonAsync("api/books", dto);

        //Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateBookAsync_DatePublishedInTheFuture_ReturnsBadRequest()
    {
        //Arrange
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var author = await new AuthorSeeder(db).SeedOneAsync();
        var dto = new BookCreateDto
        {
            Title = "Future Book",
            Description = "A book from tomorrow.",
            Isbn = "9781234567891",
            AuthorId = author.Id,
            DatePublished = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)),
            Rating = 4.0m
        };

        //Act
        var response = await Client.PostAsJsonAsync("api/books", dto);

        //Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("", "9781234567890")]
    [InlineData("Valid Title", "")]
    [InlineData("Valid Title", "123")]
    public async Task CreateBookAsync_MissingRequiredField_ReturnsBadRequest(string title, string isbn)
    {
        //Arrange
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var author = await new AuthorSeeder(db).SeedOneAsync();
        var dto = new BookCreateDto
        {
            Title = title,
            Description = "A valid description.",
            Isbn = isbn,
            AuthorId = author.Id,
            DatePublished = DateOnly.FromDateTime(DateTime.UtcNow),
            Rating = 4.0m
        };

        //Act
        var response = await Client.PostAsJsonAsync("api/books", dto);

        //Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateBookAsync_DuplicateIsbn_ReturnsExpectedStatus()
    {
        //Arrange
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var seeder = new BookSeeder(db);
        var existingBook = await seeder.SeedOneAsync(o => o.Isbn = "9781111111111");
        var author = await new AuthorSeeder(db).SeedOneAsync();
        var dto = new BookCreateDto
        {
            Title = "Duplicate ISBN Book",
            Description = "Testing duplicate ISBN insertion.",
            Isbn = existingBook.Isbn,
            AuthorId = author.Id,
            DatePublished = DateOnly.FromDateTime(DateTime.UtcNow),
            Rating = 3.5m
        };

        //Act
        var response = await Client.PostAsJsonAsync("api/books", dto);

        //Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task UpdateBookAsync_ExistingBook_ReturnsNoContentAndPersistsChange()
    {
        //Arrange
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var seeder = new BookSeeder(db);
        var book = await seeder.SeedOneAsync();
        var dto = new BookEditDto
        {
            Title = "Updated Book Title",
            Description = "Updated Description",
            DatePublished = new DateOnly(2020, 1, 1),
            Rating = 5.0m
        };

        //Act
        var response = await Client.PutAsJsonAsync($"api/books/{book.Id}", dto);

        //Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        using var assertScope = Factory.Services.CreateScope();
        var assertDb = assertScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var updated = await assertDb.Books.FindAsync(book.Id);
        Assert.NotNull(updated);
        Assert.Equal("Updated Book Title", updated.Title);
    }

    [Fact]
    public async Task UpdateBookAsync_NonExistingId_ReturnsNotFound()
    {
        //Arrange
        var id = Guid.CreateVersion7();
        var dto = new BookEditDto
        {
            Title = "Updated",
            Description = "Desc",
            DatePublished = new DateOnly(2020, 1, 1),
            Rating = 4m
        };

        //Act
        var response = await Client.PutAsJsonAsync($"api/books/{id}", dto);

        //Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateBookAsync_DeletedBook_ReturnsNotFound()
    {
        //Arrange
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var seeder = new BookSeeder(db);
        var book = await seeder.SeedOneAsync(o => o.IsDeleted = true);
        var dto = new BookEditDto
        {
            Title = "Updated",
            Description = "Desc",
            DatePublished = new DateOnly(2020, 1, 1),
            Rating = 4m
        };

        //Act
        var response = await Client.PutAsJsonAsync($"api/books/{book.Id}", dto);

        //Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateBookAsync_DatePublishedInTheFuture_ReturnsBadRequest()
    {
        //Arrange
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var seeder = new BookSeeder(db);
        var book = await seeder.SeedOneAsync();
        var dto = new BookEditDto
        {
            Title = "Updated Book",
            Description = "Desc",
            DatePublished = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)),
            Rating = 4m
        };

        //Act
        var response = await Client.PutAsJsonAsync($"api/books/{book.Id}", dto);

        //Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateBookRatingAsync_ExistingBook_ReturnsNoContentAndPersistsRating()
    {
        //Arrange
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var seeder = new BookSeeder(db);
        var book = await seeder.SeedOneAsync(o => o.Rating = 2.0m);
        var ratingDto = new BookRatingDto { Rating = 4.75m };

        //Act
        var response = await Client.PatchAsJsonAsync($"api/books/rating/{book.Id}", ratingDto);

        //Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        using var assertScope = Factory.Services.CreateScope();
        var assertDb = assertScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var updated = await assertDb.Books.FindAsync(book.Id);
        Assert.NotNull(updated);
        Assert.Equal(4.75m, updated.Rating);
    }

    [Fact]
    public async Task UpdateBookRatingAsync_NonExistingId_ReturnsNotFound()
    {
        //Arrange
        var id = Guid.CreateVersion7();
        var ratingDto = new BookRatingDto { Rating = 4.0m };

        //Act
        var response = await Client.PatchAsJsonAsync($"api/books/rating/{id}", ratingDto);

        //Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateBookRatingAsync_RatingOutOfRange_ReturnsBadRequest()
    {
        //Arrange
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var seeder = new BookSeeder(db);
        var book = await seeder.SeedOneAsync();
        var ratingDto = new BookRatingDto { Rating = 6.0m };

        //Act
        var response = await Client.PatchAsJsonAsync($"api/books/rating/{book.Id}", ratingDto);

        //Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DeleteBookAsync_ExistingBook_ReturnsNoContent()
    {
        //Arrange
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var seeder = new BookSeeder(db);
        var book = await seeder.SeedOneAsync();

        //Act
        var response = await Client.DeleteAsync($"api/books/{book.Id}");

        //Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task DeleteBookAsync_NonExistingId_ReturnsNotFound()
    {
        //Arrange
        var id = Guid.CreateVersion7();

        //Act
        var response = await Client.DeleteAsync($"api/books/{id}");

        //Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteBookAsync_AlreadyDeletedBook_ReturnsNotFound()
    {
        //Arrange
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var seeder = new BookSeeder(db);
        var book = await seeder.SeedOneAsync(o => o.IsDeleted = true);

        //Act
        var response = await Client.DeleteAsync($"api/books/{book.Id}");

        //Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}