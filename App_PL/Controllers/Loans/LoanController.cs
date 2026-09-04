using App_BLL.Common.Result;
using App_BLL.Dtos.LoansDtos;
using App_BLL.QueryParams.Loan;
using App_BLL.Services.Abstraction.Loans;
using App_PL.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace App_PL.Controllers.Loans;

[ApiController]
[Route("api/[controller]")]
public class LoanController : ControllerBase
{
    private readonly ILoanService _loanService;

    public LoanController(ILoanService loanService)
    {
        _loanService = loanService;
    }

    [HttpGet]
    public async Task<IActionResult> GetLoansAsync([FromQuery] LoanQueryParams query, CancellationToken cancellationToken)
    {
        var result = await _loanService.GetLoansAsync(query, cancellationToken);
        return result.IsSuccess ? Ok(result.Data) : HandleFailure(result);
    }

    [HttpGet("{id:guid}",Name = "GetLoanById")]
    public async Task<IActionResult> GetLoanByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await _loanService.GetLoanByIdAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(result.Data) : HandleFailure(result);
    }

    [HttpPost]
    public async Task<IActionResult> LoanBookAsync(LoanCreateDto loan, CancellationToken cancellationToken)
    {
        var result = await _loanService.LoanBookAsync(loan, cancellationToken);
        return result.IsSuccess ? CreatedAtAction("GetLoanById", new { id = result.Data }, new { id = result.Data }) : HandleFailure(result);
    }

    [HttpPatch("returnLoan/{id:guid}")]
    public async Task<IActionResult> ReturnLoanAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await _loanService.ReturnBookAsync(id, cancellationToken);
        return result.IsSuccess ? NoContent() : HandleFailure(result);
    }
    
    private IActionResult HandleFailure(Result result) => 
        Problem(detail: result.Message, statusCode: result.Error!.Value.ToHttpStatusCode());
    
}