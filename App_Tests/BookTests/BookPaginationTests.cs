using App_BLL.Common.Paging;
using App_BLL.QueryParams.Book;
using App_Common.Common.Book;
using App_DAL.Entities.Books;
using App_DAL.Repos.Implementaion.Books;

namespace App_Tests.BookTests;

public class BookPaginationTests
{
    private readonly DateOnly _today = DateOnly.FromDateTime(DateTime.UtcNow);

    private async Task<InMemoryBookRepo> CreatePopulatedRepoAsync(int count)
    {
        var repo = new InMemoryBookRepo();
        for (int i = 1; i <= count; i++)
        {
            var status = i % 2 == 0 ? BookStatus.Finished : BookStatus.Started;
            var rating = i % 2 == 0 ? 4.5m : 3.0m;
            await repo.AddBookAsync(new Book($"Book {i:D2}", $"Description {i}", $"Author {i}", _today, rating, status));
        }
        return repo;
    }

    //test GetAllBooksAsync returns first page items and correct total count
    [Fact]
    public async Task GetAllBooksAsync_FirstPage_ReturnsExpectedItemsAndTotalCount()
    {
        //Arrange
        var repo = await CreatePopulatedRepoAsync(15);
        var query = new BookQuery { PageNumber = 1, PageSize = 5 };

        //Act
        var (items, totalCount) = await repo.GetAllBooksAsync(query);

        //Assert
        Assert.Equal(15, totalCount);
        Assert.Equal(5, items.Count);
    }

    //test GetAllBooksAsync returns second page items skipping previous page
    [Fact]
    public async Task GetAllBooksAsync_SecondPage_ReturnsNextItems()
    {
        //Arrange
        var repo = await CreatePopulatedRepoAsync(10);
        var firstPageQuery = new BookQuery { PageNumber = 1, PageSize = 5 };
        var secondPageQuery = new BookQuery { PageNumber = 2, PageSize = 5 };

        //Act
        var (firstPageItems, _) = await repo.GetAllBooksAsync(firstPageQuery);
        var (secondPageItems, totalCount) = await repo.GetAllBooksAsync(secondPageQuery);

        //Assert
        Assert.Equal(10, totalCount);
        Assert.Equal(5, secondPageItems.Count);
        Assert.Empty(firstPageItems.Intersect(secondPageItems));
    }

    //test GetAllBooksAsync respects requested page size
    [Theory]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(10)]
    public async Task GetAllBooksAsync_PageSizeSpecified_ReturnsExactNumberOfItems(int pageSize)
    {
        //Arrange
        var repo = await CreatePopulatedRepoAsync(20);
        var query = new BookQuery { PageNumber = 1, PageSize = pageSize };

        //Act
        var (items, totalCount) = await repo.GetAllBooksAsync(query);

        //Assert
        Assert.Equal(20, totalCount);
        Assert.Equal(pageSize, items.Count);
    }

    //test GetAllBooksAsync returns empty list when page number is beyond available data
    [Fact]
    public async Task GetAllBooksAsync_PageBeyondAvailableData_ReturnsEmptyList()
    {
        //Arrange
        var repo = await CreatePopulatedRepoAsync(5);
        var query = new BookQuery { PageNumber = 3, PageSize = 5 };

        //Act
        var (items, totalCount) = await repo.GetAllBooksAsync(query);

        //Assert
        Assert.Equal(5, totalCount);
        Assert.Empty(items);
    }

    //test GetAllBooksAsync excludes deleted books from both items and total count
    [Fact]
    public async Task GetAllBooksAsync_WithDeletedBooks_ExcludesDeletedFromResultsAndTotalCount()
    {
        //Arrange
        var repo = new InMemoryBookRepo();
        var activeBook1 = new Book("Active 1", "Desc", "Author", _today, 4.0m, BookStatus.Started);
        var activeBook2 = new Book("Active 2", "Desc", "Author", _today, 4.0m, BookStatus.Started);
        var deletedBook = new Book("Deleted", "Desc", "Author", _today, 4.0m, BookStatus.Started);

        await repo.AddBookAsync(activeBook1);
        await repo.AddBookAsync(activeBook2);
        await repo.AddBookAsync(deletedBook);
        await repo.DeleteBookAsync(deletedBook.Id);

        var query = new BookQuery { PageNumber = 1, PageSize = 10 };

        //Act
        var (items, totalCount) = await repo.GetAllBooksAsync(query);

        //Assert
        Assert.Equal(2, totalCount);
        Assert.Equal(2, items.Count);
        Assert.DoesNotContain(items, b => b.Id == deletedBook.Id);
    }

    //test GetAllBooksAsync applies filters before pagination and returns filtered total count
    [Fact]
    public async Task GetAllBooksAsync_FiltersAndPaginationCombined_ReturnsCorrectPagedFilteredResults()
    {
        //Arrange
        var repo = await CreatePopulatedRepoAsync(20);
        // Odd numbers are BookStatus.Started (10 items), even numbers are BookStatus.Finished (10 items)
        var query = new BookQuery
        {
            Status = BookStatus.Finished,
            PageNumber = 1,
            PageSize = 4
        };

        //Act
        var (items, totalCount) = await repo.GetAllBooksAsync(query);

        //Assert
        Assert.Equal(10, totalCount);
        Assert.Equal(4, items.Count);
        Assert.All(items, b => Assert.Equal(BookStatus.Finished, b.Status));
    }

    //test BookQueryParams clamps PageSize to MaxPageSize of 50 when exceeded
    [Theory]
    [InlineData(51, 50)]
    [InlineData(100, 50)]
    [InlineData(50, 50)]
    [InlineData(25, 25)]
    public void BookQueryParams_PageSizeExceedingMax_ClampsToMaxPageSize(int requestedPageSize, int expectedPageSize)
    {
        //Arrange & Act
        var queryParams = new BookQueryParams { PageSize = requestedPageSize };

        //Assert
        Assert.Equal(expectedPageSize, queryParams.PageSize);
    }

    //test BookQueryParams defaults to page 1 and page size 10
    [Fact]
    public void BookQueryParams_DefaultValues_SetsPageNumberOneAndPageSizeTen()
    {
        //Arrange & Act
        var queryParams = new BookQueryParams();

        //Assert
        Assert.Equal(1, queryParams.PageNumber);
        Assert.Equal(10, queryParams.PageSize);
    }

    //test PagedResult calculates TotalPages correctly
    [Theory]
    [InlineData(0, 10, 0)]
    [InlineData(5, 10, 1)]
    [InlineData(10, 10, 1)]
    [InlineData(11, 10, 2)]
    [InlineData(25, 10, 3)]
    public void PagedResult_TotalPages_CalculatesExpectedPages(int totalCount, int pageSize, int expectedTotalPages)
    {
        //Arrange & Act
        var pagedResult = new PagedResult<string>
        {
            TotalCount = totalCount,
            PageSize = pageSize
        };

        //Assert
        Assert.Equal(expectedTotalPages, pagedResult.TotalPages);
    }
}
