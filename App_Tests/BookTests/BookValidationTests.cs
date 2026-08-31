using System.ComponentModel.DataAnnotations;
using App_BLL.Dtos.BooksDtos;
using App_BLL.QueryParams.Book;
namespace App_Tests.BookTests;

public class BookValidationTests
{
    private readonly DateOnly _today = DateOnly.FromDateTime(DateTime.UtcNow);

    private static IList<ValidationResult> ValidateModel(object model)
    {
        var validationResults = new List<ValidationResult>();
        var ctx = new ValidationContext(model, serviceProvider: null, items: null);
        Validator.TryValidateObject(model, ctx, validationResults, validateAllProperties: true);
        return validationResults;
    }

    private BookCreateDto CreateValidBookCreateDto()
    {
        return new BookCreateDto
        {
            Title = "Clean Code",
            Description = "A handbook of agile software craftsmanship",
            AuthorName = "Robert C. Martin",
            DatePublished = _today,
            Rating = 4.5m
        };
    }

    private BookEditDto CreateValidBookEditDto()
    {
        return new BookEditDto
        {
            Title = "Clean Code",
            Description = "A handbook of agile software craftsmanship",
            AuthorName = "Robert C. Martin",
            DatePublished = _today,
            Rating = 4.5m
        };
    }

    //test BookCreateDto valid model passes validation
    [Fact]
    public void BookCreateDto_ValidModel_PassesValidation()
    {
        //Arrange
        var dto = CreateValidBookCreateDto();

        //Act
        var results = ValidateModel(dto);

        //Assert
        Assert.Empty(results);
    }

    //test BookCreateDto fails when required string fields are null or empty
    [Theory]
    [InlineData(null, "Description", "Author")]
    [InlineData("Title", null, "Author")]
    [InlineData("Title", "Description", null)]
    public void BookCreateDto_RequiredFieldsMissing_FailsValidation(string? title, string? description, string? authorName)
    {
        //Arrange
        var dto = new BookCreateDto
        {
            Title = title!,
            Description = description!,
            AuthorName = authorName!,
            DatePublished = _today,
            Rating = 4.0m
        };

        //Act
        var results = ValidateModel(dto);

        //Assert
        Assert.NotEmpty(results);
    }

    //test BookCreateDto fails when string lengths exceed MaxLength attributes
    [Theory]
    [InlineData(101, 50, 50, nameof(BookCreateDto.Title))]
    [InlineData(50, 301, 50, nameof(BookCreateDto.Description))]
    [InlineData(50, 50, 71, nameof(BookCreateDto.AuthorName))]
    public void BookCreateDto_ExceedsMaxLength_FailsValidation(int titleLen, int descLen, int authorLen, string expectedErrorMember)
    {
        //Arrange
        var dto = new BookCreateDto
        {
            Title = new string('a', titleLen),
            Description = new string('b', descLen),
            AuthorName = new string('c', authorLen),
            DatePublished = _today,
            Rating = 4.0m
        };

        //Act
        var results = ValidateModel(dto);

        //Assert
        Assert.Contains(results, r => r.MemberNames.Contains(expectedErrorMember));
    }

    //test BookCreateDto fails when rating is out of 0 to 5 range
    [Theory]
    [InlineData(-1.0)]
    [InlineData(5.5)]
    [InlineData(10.0)]
    public void BookCreateDto_RatingOutOfRange_FailsValidation(decimal rating)
    {
        //Arrange
        var dto = CreateValidBookCreateDto();
        dto.Rating = rating;

        //Act
        var results = ValidateModel(dto);

        //Assert
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(BookCreateDto.Rating)));
    }

    //test BookEditDto valid model passes validation
    [Fact]
    public void BookEditDto_ValidModel_PassesValidation()
    {
        //Arrange
        var dto = CreateValidBookEditDto();

        //Act
        var results = ValidateModel(dto);

        //Assert
        Assert.Empty(results);
    }

    //test BookEditDto fails when required fields are missing
    [Theory]
    [InlineData(null, "Description", "Author")]
    [InlineData("Title", null, "Author")]
    [InlineData("Title", "Description", null)]
    public void BookEditDto_RequiredFieldsMissing_FailsValidation(string? title, string? description, string? authorName)
    {
        //Arrange
        var dto = new BookEditDto
        {
            Title = title!,
            Description = description!,
            AuthorName = authorName!,
            DatePublished = _today,
            Rating = 4.0m
        };

        //Act
        var results = ValidateModel(dto);

        //Assert
        Assert.NotEmpty(results);
    }

    //test BookEditDto fails when rating is out of range
    [Theory]
    [InlineData(-1.0)]
    [InlineData(5.5)]
    public void BookEditDto_RatingOutOfRange_FailsValidation(decimal rating)
    {
        //Arrange
        var dto = CreateValidBookEditDto();
        dto.Rating = rating;

        //Act
        var results = ValidateModel(dto);

        //Assert
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(BookEditDto.Rating)));
    }

    //test BookRatingDto passes validation for ratings within 0 to 5
    [Theory]
    [InlineData(0)]
    [InlineData(2.5)]
    [InlineData(5)]
    public void BookRatingDto_ValidRating_PassesValidation(decimal rating)
    {
        //Arrange
        var dto = new BookRatingDto { Rating = rating };

        //Act
        var results = ValidateModel(dto);

        //Assert
        Assert.Empty(results);
    }

    //test BookRatingDto fails validation for ratings out of range
    [Theory]
    [InlineData(-1.0)]
    [InlineData(6.0)]
    public void BookRatingDto_OutOfRangeRating_FailsValidation(decimal rating)
    {
        //Arrange
        var dto = new BookRatingDto { Rating = rating };

        //Act
        var results = ValidateModel(dto);

        //Assert
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(BookRatingDto.Rating)));
    }

    //test BookQueryParams passes validation with default and valid values
    [Fact]
    public void BookQueryParams_ValidValues_PassesValidation()
    {
        //Arrange
        var query = new BookQueryParams
        {
            Title = "Clean Code",
            Rating = 4.5m
        };

        //Act
        var results = ValidateModel(query);

        //Assert
        Assert.Empty(results);
    }

    //test BookQueryParams fails validation when title exceeds 100 characters
    [Fact]
    public void BookQueryParams_TitleExceedsMaxLength_FailsValidation()
    {
        //Arrange
        var query = new BookQueryParams
        {
            Title = new string('a', 101)
        };

        //Act
        var results = ValidateModel(query);

        //Assert
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(BookQueryParams.Title)));
    }

    //test BookQueryParams fails validation when rating is out of 0 to 5 range
    [Theory]
    [InlineData(-1.0)]
    [InlineData(6.0)]
    public void BookQueryParams_RatingOutOfRange_FailsValidation(decimal rating)
    {
        //Arrange
        var query = new BookQueryParams { Rating = rating };

        //Act
        var results = ValidateModel(query);

        //Assert
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(BookQueryParams.Rating)));
    }
}
