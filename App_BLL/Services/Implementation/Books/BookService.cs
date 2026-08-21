using App_BLL.Common.Paging;
using App_BLL.Common.Result;
using App_BLL.Dtos.BooksDtos;
using App_BLL.QueryParams.Book;
using App_BLL.Services.Abstraction.Books;
using App_Common.Common.Book;
using App_DAL.Entities.Books;
using App_DAL.Repos.Abstraction.Books;
using AutoMapper;
using Microsoft.Extensions.Logging;

namespace App_BLL.Services.Implementation.Books;



public class BookService : IBookService
{
    private readonly IBookRepo _bookRepo;
    private readonly IMapper _mapper;
    private readonly ILogger<BookService> _logger;
    public BookService(IBookRepo repo, IMapper mapper,  ILogger<BookService> logger)
    {
        _bookRepo = repo;
        _mapper = mapper;
        _logger = logger;
        
    }
    public async Task<Result<PagedResult<BookGetDto>>> GetAllBooksAsync(BookQueryParams  query)
    {
        
        var bookQuery = new BookQuery
        {
            PageNumber = query.PageNumber,
            PageSize = query.PageSize,
        };

        var (books, totalCount) = await _bookRepo.GetAllBooksAsync(bookQuery);
        var dtos = _mapper.Map<IReadOnlyList<BookGetDto>>(books);

        return Result<PagedResult<BookGetDto>>.Success(new PagedResult<BookGetDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            PageNumber = query.PageNumber,
            PageSize = query.PageSize
        });
        
    }

    public async Task<Result<BookGetDto>> GetBookAsync(Guid id)
    {
        var book = await _bookRepo.GetBookByIdAsync(id);
        if (book is null)
            return Result<BookGetDto>.Failed(ErrorType.NotFound, "Book Not Found");

        return Result<BookGetDto>.Success(_mapper.Map<BookGetDto>(book));
    }

    public async Task<Result<Guid>> AddBookAsync(BookCreateDto book)
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        if (book.DatePublished > today)
        {
            _logger.LogWarning("Adding Book Failed : Date Published {DatePublished} Can't be in the future", book.DatePublished);
            return Result<Guid>.Failed(ErrorType.BadRequest, "Book Date Published Can't be in the future");
        }
        var newBook = _mapper.Map<Book>(book);
        await _bookRepo.AddBookAsync(newBook);
        _logger.LogInformation("Book {BookId} Added Successfully", newBook.Id);
        return Result<Guid>.Success(newBook.Id);
    }

    public async Task<Result> UpdateBookAsync(BookEditDto book, Guid editedBookId)
    {
        var bookToUpdate = await _bookRepo.GetBookByIdAsync(editedBookId);
        if (bookToUpdate == null)
        {
            _logger.LogWarning("Update Book Failed : Book {BookId} Not Found", editedBookId);
            return Result.Failed(ErrorType.NotFound, "Book Not Found");
        }else if (bookToUpdate.IsDeleted)
        {
            _logger.LogWarning("Update Book Failed : Book {BookId} Is Deleted", editedBookId);
            return Result.Failed(ErrorType.NotFound, "Book is Deleted");
        }
        var today = DateOnly.FromDateTime(DateTime.Now);
        if (book.DatePublished > today)
        {
            _logger.LogWarning("Update Book Failed : Date Published {DatePublished} Can't be in the future", book.DatePublished);
            return Result.Failed(ErrorType.BadRequest, "Book Date Published Can't be in the future");
        }
        bookToUpdate.UpdateBook(book.Title,book.Description,book.AuthorName,book.DatePublished,book.Rating,book.Status);
        _logger.LogInformation("Book {BookId} Updated", editedBookId);
        return Result.Success("Book Updated");
    }

    public async Task<Result> UpdateBookStatusAsync(Guid id, BookStatusDto status)
    {
        var bookToUpdate = await _bookRepo.GetBookByIdAsync(id);
        if (bookToUpdate == null)
        {
            _logger.LogWarning("Update Book Status Failed : Book {BookId} Not Found", id);
            return Result.Failed(ErrorType.NotFound, "Book Not Found");
        }else if (bookToUpdate.IsDeleted)
        {
            _logger.LogWarning("Update Book Status Failed : Book {BookId} Is Deleted", id);
            return Result.Failed(ErrorType.NotFound, "Book is Deleted");
        }

        var bookToUpdateStatus = status.Status;
        bookToUpdate.UpdateStatus(bookToUpdateStatus);
        _logger.LogInformation("Book {BookId} Status Updated to {Status}", id, bookToUpdateStatus);
        return Result.Success("Book Status Updated");
    }

    public async Task<Result> UpdateBookRatingAsync(Guid id, decimal rating)
    {
        var bookToUpdate = await _bookRepo.GetBookByIdAsync(id);
        if (bookToUpdate == null)
        {
            _logger.LogWarning("Update Book Rating Failed : Book {BookId} Not Found", id);
            return Result.Failed(ErrorType.NotFound, "Book Not Found");
        }else if (bookToUpdate.IsDeleted)
        {
            _logger.LogWarning("Update Book Rating Failed : Book {BookId} Is Deleted", id);
            return Result.Failed(ErrorType.NotFound, "Book is Deleted");
        }
        bookToUpdate.UpdateRating(rating);
        _logger.LogInformation("Book {BookId} Rating Updated to {Rating}", id , rating);
        return Result.Success("Book Ratings Updated");
    }

    public async Task<Result> DeleteBookAsync(Guid id)
    {
        var bookToDelete = await _bookRepo.GetBookByIdAsync(id);
        if (bookToDelete == null)
        {
            _logger.LogWarning("Deleting Book Failed : Book {BookId} Not Found", id);
            return Result.Failed(ErrorType.NotFound, "Book Not Found");
        }else if (bookToDelete.IsDeleted)
        {
            _logger.LogWarning("Deleting Book Failed : Book {BookId} is already Deleted", id);
            return Result.Failed(ErrorType.NotFound, "Book is already Deleted");
        } 
        await _bookRepo.DeleteBookAsync(bookToDelete.Id);
        _logger.LogInformation("Book {BookId} Deleted", id);
        return Result.Success("Book Deleted");
    }
    
}