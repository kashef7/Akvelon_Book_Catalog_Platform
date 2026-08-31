using App_BLL.Common.Paging;
using App_BLL.Common.Result;
using App_BLL.Dtos.LoansDtos;
using App_BLL.QueryParams.Loan;
using App_BLL.Services.Abstraction.Loans;
using App_Common.Common.Loan;
using App_DAL.Entities.Loans;
using App_DAL.Repos.Abstraction.Books;
using App_DAL.Repos.Abstraction.Loans;
using App_DAL.Repos.Abstraction.Users;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace App_BLL.Services.Implementation.Loans;

public class LoanService : ILoanService
{
    private readonly ILoanRepo _loanRepo;
    private readonly IUserRepo _userRepo;
    private readonly IBookRepo _bookRepo;
    private readonly IMapper _mapper;
    private readonly ILogger<LoanService> _logger;

    public LoanService(ILoanRepo loanRepo, IUserRepo userRepo, IBookRepo bookRepo, IMapper mapper,
        ILogger<LoanService> logger)
    {
        _loanRepo = loanRepo;
        _userRepo = userRepo;
        _bookRepo = bookRepo;
        _mapper = mapper;
        _logger = logger;
    }
    
    public async Task<Result<PagedResult<LoanGetDto>>> GetLoansAsync(LoanQueryParams query)
    {
        var loanQuery = _mapper.Map<LoanQuery>(query);
        var (items,totalCount) = await _loanRepo.GetAllLoansAsync(loanQuery);
        
        var resultItems = _mapper.Map<IReadOnlyList<LoanGetDto>>(items);
        return Result<PagedResult<LoanGetDto>>.Success(new PagedResult<LoanGetDto>()
        {
            Items = resultItems,
            TotalCount = totalCount,
            PageNumber = query.PageNumber,
            PageSize = query.PageSize
        });

    }

    public async Task<Result<LoanGetDto>> GetLoanByIdAsync(Guid id)
    {
        var  loan = await _loanRepo.GetLoanByIdAsync(id);
        if (loan == null)
        {
            _logger.LogError("Loan with id {LoanId} not found",id);
            return Result<LoanGetDto>.Failed(ErrorType.NotFound,"Loan Not Found");
        }
        return Result<LoanGetDto>.Success(_mapper.Map<LoanGetDto>(loan));
    }

    public async Task<Result<Guid>> LoanBookAsync(LoanCreateDto loanCreateDto)
    {
        var book =  await _bookRepo.GetBookByIdAsync(loanCreateDto.BookId);
        if (book == null)
        {
            _logger.LogError("Loaning book failed : Book with id {BookId} not found",loanCreateDto.BookId);
            return Result<Guid>.Failed(ErrorType.NotFound,"Book Not Found");
        }
        var user = await _userRepo.GetUserByIdAsync(loanCreateDto.UserId);
        if (user == null)
        {
            _logger.LogError("Loaning book failed : User with id {UserId} not found",loanCreateDto.UserId);
            return Result<Guid>.Failed(ErrorType.NotFound,"User Not Found");
        }

        if (loanCreateDto.DueAt < DateTime.UtcNow)
        {
            _logger.LogWarning("Loaning book failed : Due At Date {Date} Can't be in the Past",loanCreateDto.DueAt);
            return  Result<Guid>.Failed(ErrorType.BadRequest,"Due At Date in the Past");
        }

        if (await _loanRepo.HasActiveLoanAsync(loanCreateDto.BookId))
        {
            _logger.LogWarning("Loaning book failed : Book {BookId} is already Loaned",loanCreateDto.BookId);
            return Result<Guid>.Failed(ErrorType.Conflict,"Book already Loaned");
        }
        var newLoan = new Loan(loanCreateDto.DueAt, book, user);
        try
        {
            await _loanRepo.AddLoanAsync(newLoan);
            return Result<Guid>.Success(newLoan.Id,"Book Loaned Successfully");
        }
        catch (DbUpdateException e)
        {
            _logger.LogWarning(e, "Loaning book failed : concurrent loan conflict for book {BookId}", loanCreateDto.BookId);
            return Result<Guid>.Failed(ErrorType.Conflict, "This book is no longer available.");
        }
    }

    public async Task<Result> ReturnBookAsync(Guid id)
    {
        var loan = await _loanRepo.GetLoanByIdAsync(id);
        if (loan == null)
        {
            _logger.LogError("Returning Book Failed : Loan with id {LoanId} not found",id);
            return Result.Failed(ErrorType.NotFound,"Loan Not Found");
        }

        if (loan.ReturnedAt != null)
        {
            _logger.LogWarning("Returning book Failed: Book already returned");
            return Result.Failed(ErrorType.BadRequest,"Book already returned");
        }
        loan.ReturnBook();
        await _loanRepo.SaveChangesAsync();
        return Result.Success();
    }
}