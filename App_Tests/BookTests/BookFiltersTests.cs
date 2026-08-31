using App_Common.Common.Book;
using App_DAL.Entities.Authors;
using App_DAL.Entities.Books;
using App_DAL.Filters.Books;

namespace App_Tests.BookTests;

public class BookFiltersTests
{
    private readonly DateOnly _today = DateOnly.FromDateTime(DateTime.UtcNow);
    private readonly Author _author1 = new Author("Robert C. Martin");
    private readonly Author _author2 = new Author("Erich Gamma");
    private readonly Author _author3 = new Author("Martin Fowler");
    private readonly Author _author4 = new Author("Andrew Hunt");

    private List<Book> CreateSampleBooks()
    {
        return new List<Book>
        {
            new Book("9780132350884", "Clean Code", "A handbook of agile software craftsmanship", _author1, _today, 4.8m),
            new Book("9780201633610", "Design Patterns", "Elements of Reusable Object-Oriented Software", _author2, _today, 4.5m),
            new Book("9780201485677", "Refactoring", "Improving the Design of Existing Code", _author3, _today, 4.8m),
            new Book("9780201616224", "The Pragmatic Programmer", "From Journeyman to Master", _author4, _today, 4.0m)
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

    //test ApplyQueryFilters returns only matching books when isbn filter is passed
    [Fact]
    public void ApplyQueryFilters_IsbnFilterPassed_ReturnsMatchingBooksOnly()
    {
        //Arrange
        var books = CreateSampleBooks().AsQueryable();
        var query = new BookQuery { Isbn = "9780132350884" };

        //Act
        var result = books.ApplyQueryFilters(query).ToList();

        //Assert
        Assert.Single(result);
        Assert.Equal("Clean Code", result[0].Title);
    }

    //test ApplyQueryFilters returns only matching books when authorId filter is passed
    [Fact]
    public void ApplyQueryFilters_AuthorIdFilterPassed_ReturnsMatchingBooksOnly()
    {
        //Arrange
        var books = CreateSampleBooks().AsQueryable();
        var query = new BookQuery { AuthorId = _author2.Id };

        //Act
        var result = books.ApplyQueryFilters(query).ToList();

        //Assert
        Assert.Single(result);
        Assert.Equal("Design Patterns", result[0].Title);
    }

    //test ApplyQueryFilters returns books matching min rating filter
    [Fact]
    public void ApplyQueryFilters_MinRatingFilterPassed_ReturnsMatchingBooksOnly()
    {
        //Arrange
        var books = CreateSampleBooks().AsQueryable();
        var query = new BookQuery { MinRating = 4.6m };

        //Act
        var result = books.ApplyQueryFilters(query).ToList();

        //Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, b => Assert.True(b.Rating >= 4.6m));
    }

    //test ApplyQueryFilters returns books matching max rating filter
    [Fact]
    public void ApplyQueryFilters_MaxRatingFilterPassed_ReturnsMatchingBooksOnly()
    {
        //Arrange
        var books = CreateSampleBooks().AsQueryable();
        var query = new BookQuery { MaxRating = 4.5m };

        //Act
        var result = books.ApplyQueryFilters(query).ToList();

        //Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, b => Assert.True(b.Rating <= 4.5m));
    }

    //test ApplyQueryFilters returns books within min and max rating range
    [Fact]
    public void ApplyQueryFilters_RatingRangePassed_ReturnsMatchingBooksOnly()
    {
        //Arrange
        var books = CreateSampleBooks().AsQueryable();
        var query = new BookQuery { MinRating = 4.2m, MaxRating = 4.6m };

        //Act
        var result = books.ApplyQueryFilters(query).ToList();

        //Assert
        Assert.Single(result);
        Assert.Equal("Design Patterns", result[0].Title);
    }

    //test ApplyQueryFilters returns books matching author name filter
    [Fact]
    public void ApplyQueryFilters_AuthorNameFilterPassed_ReturnsMatchingBooksOnly()
    {
        //Arrange
        var books = CreateSampleBooks().AsQueryable();
        var query = new BookQuery { AuthorName = "Fowler" };

        //Act
        var result = books.ApplyQueryFilters(query).ToList();

        //Assert
        Assert.Single(result);
        Assert.Equal("Refactoring", result[0].Title);
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
            MinRating = 4.5m,
            AuthorId = _author1.Id
        };

        //Act
        var result = books.ApplyQueryFilters(query).ToList();

        //Assert
        Assert.Single(result);
        Assert.Equal("Clean Code", result[0].Title);
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
            MinRating = 4.9m
        };

        //Act
        var result = books.ApplyQueryFilters(query).ToList();

        //Assert
        Assert.Empty(result);
    }
}
