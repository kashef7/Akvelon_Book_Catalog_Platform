using App_Common.Common.Loan;
using App_DAL.Entities.Loans;

namespace App_DAL.Filters.Loans;

public static class LoanFilters
{
    public static IQueryable<Loan> ApplyQueryFilters(this IQueryable<Loan> loans, LoanQuery loanQuery)
    {
        if (loanQuery.BookId != null)
        {
            loans = loans.Where(l => l.BookId == loanQuery.BookId);
        }

        if (loanQuery.UserId != null)
        {
            loans = loans.Where(l => l.UserId == loanQuery.UserId);
        }

        if (loanQuery.IsReturned != null)
        {
            loans = loanQuery.IsReturned.Value
                ? loans.Where(l => l.ReturnedAt != null)
                : loans.Where(l => l.ReturnedAt == null);
        }

        if (loanQuery.DueBefore != null)
        {
            loans = loans.Where(l => l.DueAt <= loanQuery.DueBefore);
        }

        if (loanQuery.DueAfter != null)
        {
            loans = loans.Where(l => l.DueAt >= loanQuery.DueAfter);
        }

        return loans;
    }
}