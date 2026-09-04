using App_BLL.Common.Paging;
using App_BLL.Common.Result;
using App_BLL.Dtos.UsersDtos;
using App_BLL.QueryParams.User;
using App_BLL.Services.Abstraction.Users;
using App_Common.Common.User;
using App_DAL.Entities.Users;
using App_DAL.Repos.Abstraction.Loans;
using App_DAL.Repos.Abstraction.Users;
using AutoMapper;
using Microsoft.Extensions.Logging;

namespace App_BLL.Services.Implementation.Users;

public class UserService : IUserService
{
    private readonly IUserRepo _userRepo;
    private readonly ILoanRepo _loanRepo;
    private readonly IMapper _mapper;
    private readonly ILogger<UserService> _logger;

    public UserService(IUserRepo userRepo, ILoanRepo loanRepo, IMapper mapper, ILogger<UserService> logger)
    {
        _userRepo = userRepo;
        _loanRepo = loanRepo;
        _mapper = mapper;
        _logger = logger;
    }
    
    public async Task<Result<PagedResult<UserGetDto>>> GetAllUsersAsync(UserQueryParams query, CancellationToken cancellationToken)
    {
        var userQuery = _mapper.Map<UserQuery>(query);
        
        var (users, totalCount) = await _userRepo.GetAllUsersAsync(userQuery, cancellationToken);
        var dtos = _mapper.Map<IReadOnlyList<UserGetDto>>(users);
        
        return Result<PagedResult<UserGetDto>>.Success(new PagedResult<UserGetDto>()
        {
            Items = dtos,
            TotalCount = totalCount,
            PageNumber = query.PageNumber,
            PageSize = query.PageSize
        });
    }

    public async Task<Result<UserGetDto>> GetUserAsync(Guid id, CancellationToken cancellationToken)
    {
        var user = await _userRepo.GetUserByIdAsync(id, cancellationToken);
        if (user == null)
        {
            _logger.LogWarning("User {UserId} Not Found", id);
            return Result<UserGetDto>.Failed(ErrorType.NotFound, "User Not Found");
        }
        return Result<UserGetDto>.Success(_mapper.Map<UserGetDto>(user));
    }

    public async Task<Result<Guid>> AddUserAsync(UserCreateDto user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var newUser = new User(user.Name);
        await _userRepo.AddUserAsync(newUser);
        return Result<Guid>.Success(newUser.Id);
    }

    public async Task<Result> UpdateUserAsync(UserEditDto user, Guid editedUserId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var editedUser = await _userRepo.GetUserByIdAsync(editedUserId, cancellationToken);
        if (editedUser == null)
        {
            _logger.LogWarning("Updating User Failed, User {UserId} Not Found", editedUserId);
            return Result.Failed(ErrorType.NotFound, "User Not Found");
        } 
        else if (editedUser.IsDeleted)
        {
            _logger.LogWarning("Updating User Failed, User {UserId} is Deleted", editedUserId);
            return Result.Failed(ErrorType.NotFound, "User Deleted");
        }
        editedUser.UpdateUser(user.Name);
        await _userRepo.SaveChangesAsync();
        return Result.Success("User Updated");
    }

    public async Task<Result> DeleteUserAsync(Guid id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var deletedUser = await _userRepo.GetUserByIdAsync(id, cancellationToken);
        if (deletedUser == null)
        {
            _logger.LogWarning("Deleting User Failed, User {UserId} Not Found", id);
            return Result.Failed(ErrorType.NotFound, "User Not Found");
        } 
        else if (deletedUser.IsDeleted)
        {
            _logger.LogWarning("Deleting User Failed, User {UserId} is Deleted", id);
            return Result.Failed(ErrorType.NotFound, "User Deleted");
        }

        if (await _loanRepo.HasActiveLoanByUserAsync(id, cancellationToken))
        {
            _logger.LogWarning("Deleting User Failed, User {UserId} has active Loans", id);
            return Result.Failed(ErrorType.Conflict, "User Has Active Loan");
        }
        deletedUser.DeleteUser();
        await _userRepo.SaveChangesAsync();
        return Result.Success("User Deleted");
    }
}
