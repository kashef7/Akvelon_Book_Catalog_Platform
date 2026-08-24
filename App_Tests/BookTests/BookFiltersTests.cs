using App_Common.Common.Book;
using App_DAL.Entities.Books;
using App_DAL.Helper;

namespace App_Tests.BookTests;

public class BookFiltersTests
{
    private readonly DateOnly _today = DateOnly.FromDateTime(DateTime.UtcNow);

    private List<Book> CreateSampleBooks()
    {
        return new List<Book>
        {
            new Book("Clean Code", "A handbook of agile software craftsmanship", "Robert C. Martin", _today, 4.8m, BookStatus.Finished),
            new Book("Design Patterns", "Elements of Reusable Object-Oriented Software", "Erich Gamma", _today, 4.5m, BookStatus.Started),
            new Book("Refactoring", "Improving the Design of Existing Code", "Martin Fowler", _today, 4.8m, BookStatus.NotStarted),
            new Book("The Pragmatic Programmer", "From Journeyman to Master", "Andrew Hunt", _today, 4.0m, BookStatus.Finished)
        };
    }

    //test ApplyQueryFilters returns all books when no filters are passed
    [Fact]
    public void ApplyQueryFilters_NoFiltersPassed_ReturnsAllBooks()
    {
        //Arrange
        var books = CreateSampleBooks().AsQueryable();
        var query = new BookQuery();

        //Act
        var result = books.ApplyQueryFilters(query).ToList();

        //Assert
        Assert.Equal(4, result.Count);
    }

    //test ApplyQueryFilters returns all books when title filter is whitespace or empty
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void ApplyQueryFilters_TitleFilterNullOrWhitespace_ReturnsAllBooks(string? title)
    {
        //Arrange
        var books = CreateSampleBooks().AsQueryable();
        var query = new BookQuery { Title = title };

        //Act
        var result = books.ApplyQueryFilters(query).ToList();

        //Assert
        Assert.Equal(4, result.Count);
    }

    //test ApplyQueryFilters returns only matching books when title filter is passed
    [Fact]
    public void ApplyQueryFilters_TitleFilterPassed_ReturnsMatchingBooksOnly()
    {
        //Arrange
        var books = CreateSampleBooks().AsQueryable();
        var query = new BookQuery { Title = "Clean Code" };

        //Act
        var result = books.ApplyQueryFilters(query).ToList();

        //Assert
        Assert.Single(result);
        Assert.Equal("Clean Code", result[0].Title);
    }

    //test ApplyQueryFilters returns empty when title filter does not match any book
    [Fact]
    public void ApplyQueryFilters_TitleFilterNotMatching_ReturnsEmpty()
    {
        //Arrange
        var books = CreateSampleBooks().AsQueryable();
        var query = new BookQuery { Title = "Unknown Book" };

        //Act
        var result = books.ApplyQueryFilters(query).ToList();

        //Assert
        Assert.Empty(result);
    }

    //test ApplyQueryFilters returns only matching books when status filter is passed
    [Theory]
    [InlineData(BookStatus.Finished, 2)]
    [InlineData(BookStatus.Started, 1)]
    [InlineData(BookStatus.NotStarted, 1)]
    public void ApplyQueryFilters_StatusFilterPassed_ReturnsMatchingBooksOnly(BookStatus status, int expectedCount)
    {
        //Arrange
        var books = CreateSampleBooks().AsQueryable();
        var query = new BookQuery { Status = status };

        //Act
        var result = books.ApplyQueryFilters(query).ToList();

        //Assert
        Assert.Equal(expectedCount, result.Count);
        Assert.All(result, b => Assert.Equal(status, b.Status));
    }

    //test ApplyQueryFilters returns only matching books when rating filter is passed
    [Fact]
    public void ApplyQueryFilters_RatingFilterPassed_ReturnsMatchingBooksOnly()
    {
        //Arrange
        var books = CreateSampleBooks().AsQueryable();
        var query = new BookQuery { Rating = 4.8m };

        //Act
        var result = books.ApplyQueryFilters(query).ToList();

        //Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, b => Assert.Equal(4.8m, b.Rating));
    }

    //test ApplyQueryFilters returns only books satisfying all filters when multiple filters are passed
    [Fact]
    public void ApplyQueryFilters_MultipleFiltersPassed_ReturnsOnlyBooksMatchingAllFilters()
    {
        //Arrange
        var books = CreateSampleBooks().AsQueryable();
        var query = new BookQuery
        {
            Title = "Clean Code",
            Status = BookStatus.Finished,
            Rating = 4.8m
        };

        //Act
        var result = books.ApplyQueryFilters(query).ToList();

        //Assert
        Assert.Single(result);
        Assert.Equal("Clean Code", result[0].Title);
        Assert.Equal(BookStatus.Finished, result[0].Status);
        Assert.Equal(4.8m, result[0].Rating);
    }

    //test ApplyQueryFilters returns empty when multiple filters together have no matches
    [Fact]
    public void ApplyQueryFilters_MultipleFiltersWithNoMatch_ReturnsEmpty()
    {
        //Arrange
        var books = CreateSampleBooks().AsQueryable();
        var query = new BookQuery
        {
            Title = "Clean Code",
            Status = BookStatus.Started,
            Rating = 4.8m
        };

        //Act
        var result = books.ApplyQueryFilters(query).ToList();

        //Assert
        Assert.Empty(result);
    }
}
