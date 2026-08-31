using App_BLL.Common.Result;
using App_BLL.Dtos.UsersDtos;
using App_BLL.QueryParams.User;
using App_BLL.Services.Abstraction.Users;
using App_BLL.Services.Implementation.Users;
using App_Common.Common.User;
using App_DAL.Entities.Users;
using App_DAL.Repos.Abstraction.Loans;
using App_DAL.Repos.Abstraction.Users;
using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace App_Tests.UserTests;

public class UserServiceTests
{
    private readonly Mock<IUserRepo> _userRepoMock;
    private readonly Mock<ILoanRepo> _loanRepoMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly IUserService _userService;

    public UserServiceTests()
    {
        _userRepoMock = new Mock<IUserRepo>();
        _loanRepoMock = new Mock<ILoanRepo>();
        _mapperMock = new Mock<IMapper>();
        _userService = new UserService(_userRepoMock.Object, _loanRepoMock.Object, _mapperMock.Object, new NullLogger<UserService>());
    }

    //test GetAllUsersAsync return correct data without filters
    [Fact]
    public async Task GetAllUsersAsync_NoFiltersPassed_ReturnsAllUsers()
    {
        //Arrange
        var userQuery = new UserQueryParams();
        IReadOnlyList<User> users = new List<User>()
        {
            new User("User1"),
            new User("User2"),
            new User("User3"),
        };
        int totalCount = users.Count;

        var mappedQuery = new UserQuery();
        IReadOnlyList<UserGetDto> userDtos = new List<UserGetDto>()
        {
            new UserGetDto { Id = Guid.NewGuid(), Name = "User1" },
            new UserGetDto { Id = Guid.NewGuid(), Name = "User2" },
            new UserGetDto { Id = Guid.NewGuid(), Name = "User3" },
        };

        _userRepoMock.Setup(repo => repo.GetAllUsersAsync(It.IsAny<UserQuery>())).ReturnsAsync((users, totalCount));
        _mapperMock.Setup(m => m.Map<UserQuery>(userQuery)).Returns(mappedQuery);
        _mapperMock.Setup(m => m.Map<IReadOnlyList<UserGetDto>>(users)).Returns(userDtos);

        //Act
        var result = await _userService.GetAllUsersAsync(userQuery);

        //Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(totalCount, result.Data!.Items.Count);
        Assert.Equal(userDtos, result.Data.Items);
        Assert.Equal(userQuery.PageNumber, result.Data.PageNumber);
        Assert.Equal(userQuery.PageSize, result.Data.PageSize);
        _mapperMock.Verify(m => m.Map<UserQuery>(userQuery), Times.Once);
        _mapperMock.Verify(m => m.Map<IReadOnlyList<UserGetDto>>(users), Times.Once);
        _userRepoMock.Verify(repo => repo.GetAllUsersAsync(mappedQuery), Times.Once);
    }

    //test GetAllUsersAsync maps and forwards filter values to the repo unchanged
    [Fact]
    public async Task GetAllUsersAsync_FiltersPassed_ForwardsMappedQueryToRepo()
    {
        //Arrange
        var userQuery = new UserQueryParams
        {
            Name = "User1"
        };
        IReadOnlyList<User> users = new List<User>()
        {
            new User("User1"),
        };
        int totalCount = users.Count;

        var mappedQuery = new UserQuery { Name = "User1" };
        IReadOnlyList<UserGetDto> userDtos = new List<UserGetDto>()
        {
            new UserGetDto { Id = Guid.NewGuid(), Name = "User1" },
        };

        _userRepoMock.Setup(repo => repo.GetAllUsersAsync(It.IsAny<UserQuery>())).ReturnsAsync((users, totalCount));
        _mapperMock.Setup(m => m.Map<UserQuery>(userQuery)).Returns(mappedQuery);
        _mapperMock.Setup(m => m.Map<IReadOnlyList<UserGetDto>>(users)).Returns(userDtos);

        //Act
        var result = await _userService.GetAllUsersAsync(userQuery);

        //Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(userDtos, result.Data!.Items);
        _mapperMock.Verify(m => m.Map<UserQuery>(userQuery), Times.Once);
        _userRepoMock.Verify(repo => repo.GetAllUsersAsync(mappedQuery), Times.Once);
    }

    //test GetUserAsync returns user when the id is found
    [Fact]
    public async Task GetUserAsync_UserFound_ReturnsMappedUser()
    {
        //Arrange
        var user = new User("User1");
        var userDto = new UserGetDto { Id = user.Id, Name = "User1" };

        _userRepoMock.Setup(repo => repo.GetUserByIdAsync(user.Id)).ReturnsAsync(user);
        _mapperMock.Setup(m => m.Map<UserGetDto>(user)).Returns(userDto);

        //Act
        var result = await _userService.GetUserAsync(user.Id);

        //Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(userDto, result.Data);
    }

    //test GetUserAsync returns not found when user with id not found
    [Fact]
    public async Task GetUserAsync_UserNotFound_ReturnsNotFound()
    {
        //Arrange
        var userId = Guid.NewGuid();
        _userRepoMock.Setup(repo => repo.GetUserByIdAsync(userId)).ReturnsAsync((User?)null);

        //Act
        var result = await _userService.GetUserAsync(userId);

        //Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.Error);
    }

    //test AddUserAsync runs correctly and sets CreatedAt
    [Fact]
    public async Task AddUserAsync_ValidUser_ReturnsSuccessWithIdAndSetsCreatedAt()
    {
        //Arrange
        var createDto = new UserCreateDto { Name = "New User" };
        User? capturedUser = null;

        _userRepoMock
            .Setup(repo => repo.AddUserAsync(It.IsAny<User>()))
            .Callback<User>(u => capturedUser = u)
            .Returns(Task.CompletedTask);

        //Act
        var result = await _userService.AddUserAsync(createDto);

        //Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(capturedUser);
        Assert.Equal(createDto.Name, capturedUser!.Name);
        Assert.NotEqual(default(DateTime), capturedUser.CreatedAt);
        Assert.Equal(capturedUser.Id, result.Data);
        _userRepoMock.Verify(repo => repo.AddUserAsync(It.IsAny<User>()), Times.Once);
    }

    //test UpdateUserAsync runs correctly with valid user data
    [Fact]
    public async Task UpdateUserAsync_ValidUser_ReturnsSuccess()
    {
        //Arrange
        var existingUser = new User("Old Name");
        var editDto = new UserEditDto { Name = "New Name" };
        _userRepoMock.Setup(repo => repo.GetUserByIdAsync(existingUser.Id)).ReturnsAsync(existingUser);

        //Act
        var result = await _userService.UpdateUserAsync(editDto, existingUser.Id);

        //Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("New Name", existingUser.Name);
        _userRepoMock.Verify(repo => repo.SaveChangesAsync(), Times.Once);
    }

    //test UpdateUserAsync returns not found if user not found
    [Fact]
    public async Task UpdateUserAsync_UserNotFound_ReturnsNotFound()
    {
        //Arrange
        var userId = Guid.NewGuid();
        var editDto = new UserEditDto { Name = "New Name" };
        _userRepoMock.Setup(repo => repo.GetUserByIdAsync(userId)).ReturnsAsync((User?)null);

        //Act
        var result = await _userService.UpdateUserAsync(editDto, userId);

        //Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.Error);
    }

    //test UpdateUserAsync returns not found if user is deleted
    [Fact]
    public async Task UpdateUserAsync_UserIsDeleted_ReturnsNotFound()
    {
        //Arrange
        var deletedUser = new User("User Name");
        deletedUser.DeleteUser();
        var editDto = new UserEditDto { Name = "New Name" };
        _userRepoMock.Setup(repo => repo.GetUserByIdAsync(deletedUser.Id)).ReturnsAsync(deletedUser);

        //Act
        var result = await _userService.UpdateUserAsync(editDto, deletedUser.Id);

        //Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.Error);
    }

    //test DeleteUserAsync runs correctly when user has no active loans
    [Fact]
    public async Task DeleteUserAsync_ValidUser_ReturnsSuccess()
    {
        //Arrange
        var existingUser = new User("User Name");
        _userRepoMock.Setup(repo => repo.GetUserByIdAsync(existingUser.Id)).ReturnsAsync(existingUser);
        _loanRepoMock.Setup(repo => repo.HasActiveLoanByUserAsync(existingUser.Id)).ReturnsAsync(false);

        //Act
        var result = await _userService.DeleteUserAsync(existingUser.Id);

        //Assert
        Assert.True(result.IsSuccess);
        Assert.True(existingUser.IsDeleted);
        _userRepoMock.Verify(repo => repo.SaveChangesAsync(), Times.Once);
    }

    //test DeleteUserAsync returns not found if user not found
    [Fact]
    public async Task DeleteUserAsync_UserNotFound_ReturnsNotFound()
    {
        //Arrange
        var userId = Guid.NewGuid();
        _userRepoMock.Setup(repo => repo.GetUserByIdAsync(userId)).ReturnsAsync((User?)null);

        //Act
        var result = await _userService.DeleteUserAsync(userId);

        //Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.Error);
    }

    //test DeleteUserAsync returns not found if user is deleted
    [Fact]
    public async Task DeleteUserAsync_UserIsDeleted_ReturnsNotFound()
    {
        //Arrange
        var deletedUser = new User("User Name");
        deletedUser.DeleteUser();
        _userRepoMock.Setup(repo => repo.GetUserByIdAsync(deletedUser.Id)).ReturnsAsync(deletedUser);

        //Act
        var result = await _userService.DeleteUserAsync(deletedUser.Id);

        //Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.Error);
    }

    //test DeleteUserAsync returns conflict if user has active loans
    [Fact]
    public async Task DeleteUserAsync_UserHasActiveLoans_ReturnsConflict()
    {
        //Arrange
        var existingUser = new User("User Name");
        _userRepoMock.Setup(repo => repo.GetUserByIdAsync(existingUser.Id)).ReturnsAsync(existingUser);
        _loanRepoMock.Setup(repo => repo.HasActiveLoanByUserAsync(existingUser.Id)).ReturnsAsync(true);

        //Act
        var result = await _userService.DeleteUserAsync(existingUser.Id);

        //Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.Error);
        Assert.False(existingUser.IsDeleted);
        _userRepoMock.Verify(repo => repo.SaveChangesAsync(), Times.Never);
    }
}
