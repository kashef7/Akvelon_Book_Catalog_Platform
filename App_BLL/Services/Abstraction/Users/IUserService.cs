using App_BLL.Common.Paging;
using App_BLL.Common.Result;
using App_BLL.Dtos.UsersDtos;
using App_BLL.QueryParams.User;

namespace App_BLL.Services.Abstraction.Users;

public interface IUserService
{
    Task<Result<PagedResult<UserGetDto>>> GetAllUsersAsync(UserQueryParams query, CancellationToken cancellationToken);
    Task<Result<UserGetDto>> GetUserAsync(Guid id, CancellationToken cancellationToken);
    Task<Result<Guid>> AddUserAsync(UserCreateDto user, CancellationToken cancellationToken);
    Task<Result> UpdateUserAsync(UserEditDto user, Guid editedUserId, CancellationToken cancellationToken);
    Task<Result> DeleteUserAsync(Guid id, CancellationToken cancellationToken);
}
