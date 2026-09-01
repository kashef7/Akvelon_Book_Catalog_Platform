using App_Common.Common.Loan;
using App_DAL.Database;
using App_DAL.Entities.Loans;
using App_DAL.Filters.Loans;
using App_DAL.Repos.Abstraction.Loans;
using Microsoft.EntityFrameworkCore;

namespace App_DAL.Repos.Implementation.Loans;

public class LoanRepo : ILoanRepo
{
    private readonly AppDbContext _dbContext;
    public LoanRepo(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    public async Task<(IReadOnlyList<Loan> items, int totalCount)> GetAllLoansAsync(LoanQuery loanQuery)
    {
        var query = _dbContext.Loans.AsQueryable().AsNoTracking()
            .ApplyQueryFilters(loanQuery)
            .Include(l => l.Book)
            .Include(l => l.User);
        var totalCount = await query.CountAsync();
        
        IReadOnlyList<Loan> result = await query.OrderBy(x => x.Id).Skip((loanQuery.PageNumber - 1) * loanQuery.PageSize).Take(loanQuery.PageSize).ToListAsync();
        return (result, totalCount);
    }

    public async Task<Loan?> GetLoanByIdAsync(Guid loanId)
    {
        var loan = await _dbContext.Loans
            .Include(l => l.Book)
            .Include(l => l.User)
            .FirstOrDefaultAsync(l => l.Id == loanId);
        return loan;
    }

    public async Task AddLoanAsync(Loan loan)
    {
        await _dbContext.Loans.AddAsync(loan);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<bool> HasActiveLoanAsync(Guid bookId)
    {
        return await _dbContext.Loans.AnyAsync(l => l.BookId == bookId && l.ReturnedAt == null);
    }

    public async Task<bool> HasActiveLoanByUserAsync(Guid userId)
    {
        return await _dbContext.Loans.AnyAsync(l => l.UserId == userId && l.ReturnedAt == null);
    }

    public async Task SaveChangesAsync()
    {
        await _dbContext.SaveChangesAsync();
    }
}