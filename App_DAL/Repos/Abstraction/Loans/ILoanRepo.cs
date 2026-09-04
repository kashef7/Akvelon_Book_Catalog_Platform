using App_Common.Common.Loan;
using App_DAL.Entities.Loans;

namespace App_DAL.Repos.Abstraction.Loans;

public interface ILoanRepo
{
    public Task<(IReadOnlyList<Loan> items,int totalCount)> GetAllLoansAsync(LoanQuery loanQuery, CancellationToken cancellationToken);
    public Task<Loan?> GetLoanByIdAsync(Guid loanId, CancellationToken cancellationToken);
    public Task AddLoanAsync(Loan loan);
    
    public Task<bool> HasActiveLoanAsync(Guid bookId, CancellationToken cancellationToken);
    public Task<bool> HasActiveLoanByUserAsync(Guid userId, CancellationToken cancellationToken);
    public Task SaveChangesAsync();
    
}