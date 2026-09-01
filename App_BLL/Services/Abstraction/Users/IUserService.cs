using App_BLL.Common.Paging;
using App_BLL.Common.Result;
using App_BLL.Dtos.UsersDtos;
using App_BLL.QueryParams.User;

namespace App_BLL.Services.Abstraction.Users;

public interface IUserService
{
    Task<Result<PagedResult<UserGetDto>>> GetAllUsersAsync(UserQueryParams query);
    Task<Result<UserGetDto>> GetUserAsync(Guid id);
    Task<Result<Guid>> AddUserAsync(UserCreateDto user);
    Task<Result> UpdateUserAsync(UserEditDto user, Guid editedUserId);
    Task<Result> DeleteUserAsync(Guid id);
}
