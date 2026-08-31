using App_Common.Common.Loan;
using App_DAL.Entities.Loans;

namespace App_DAL.Repos.Abstraction.Loans;

public interface ILoanRepo
{
    public Task<(IReadOnlyList<Loan> items,int totalCount)> GetAllLoansAsync(LoanQuery loanQuery);
    public Task<Loan?> GetLoanByIdAsync(Guid loanId);
    public Task AddLoanAsync(Loan loan);
    
    public Task<bool> HasActiveLoanAsync(Guid bookId);
    public Task<bool> HasActiveLoanByUserAsync(Guid userId);
    public Task SaveChangesAsync();
    
}