using App_Common.Common.Loan;
using App_DAL.Entities.Loans;

namespace App_DAL.Filters.Loans;

public static class LoanFilters
{
    public static IQueryable<Loan> ApplyQueryFilters(this IQueryable<Loan> loans, LoanQuery loanQuery)
    {
        return loans;
    }
}