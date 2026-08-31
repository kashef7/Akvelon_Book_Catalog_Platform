using App_BLL.Common.Paging;
using App_BLL.Common.Result;
using App_BLL.Dtos.LoansDtos;
using App_BLL.QueryParams.Loan;
using App_Common.Common.Loan;

namespace App_BLL.Services.Abstraction.Loans;

public interface ILoanService
{
    Task<Result<PagedResult<LoanGetDto>>> GetLoansAsync(LoanQueryParams query);
    Task<Result<LoanGetDto>> GetLoanByIdAsync(Guid id);
    Task<Result<Guid>> LoanBookAsync(LoanCreateDto loanCreateDto);
    Task<Result> ReturnBookAsync(Guid id);
}