using App_BLL.Common.Paging;
using App_BLL.Common.Result;
using App_BLL.Dtos.BooksDtos;
using App_BLL.QueryParams.Book;
using App_BLL.Services.Abstraction.Books;
using App_Common.Common.Book;
using App_DAL.Entities.Books;
using App_DAL.Repos.Abstraction.Authors;
using App_DAL.Repos.Abstraction.Books;
using AutoMapper;
using Microsoft.Extensions.Logging;

namespace App_BLL.Services.Implementation.Books;



public class BookService : IBookService
{
    private readonly IBookRepo _bookRepo;
    private readonly IAuthorRepo _authorRepo;
    private readonly IMapper _mapper;
    private readonly ILogger<BookService> _logger;
    public BookService(IBookRepo repo,IAuthorRepo authorRepo, IMapper mapper,  ILogger<BookService> logger)
    {
        _bookRepo = repo;
        _authorRepo = authorRepo;
        _mapper = mapper;
        _logger = logger;
        
    }
    public async Task<Result<PagedResult<BookGetDto>>> GetAllBooksAsync(BookQueryParams  query)
    {
        
        var bookQuery = _mapper.Map<BookQuery>(query);

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
        {
            _logger.LogWarning("Book {BookId} Not Found", id);
            return Result<BookGetDto>.Failed(ErrorType.NotFound, "Book Not Found");
        }

        return Result<BookGetDto>.Success(_mapper.Map<BookGetDto>(book));
    }

    public async Task<Result<BookGetDto>> GetBookByIsbnAsync(string isbn)
    {
        var book = await _bookRepo.GetBookByIsbnAsync(isbn);
        if (book is null)
        {
            _logger.LogWarning("Book with Isbn:{BookIsbn} Not Found", isbn);
            return Result<BookGetDto>.Failed(ErrorType.NotFound, "Book Not Found");
        }

        return Result<BookGetDto>.Success(_mapper.Map<BookGetDto>(book));
    }

    public async Task<Result<Guid>> AddBookAsync(BookCreateDto book)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (book.DatePublished > today)
        {
            _logger.LogWarning("Creating Book Failed : Date Published {DatePublished} Can't be in the future", book.DatePublished);
            return Result<Guid>.Failed(ErrorType.BadRequest, "Book Date Published Can't be in the future");
        }
        var author = await _authorRepo.GetAuthorByIdAsync(book.AuthorId);
        if (author == null)
        {
            _logger.LogWarning("Creating Book Failed : Author {AuthorId} Not Found", book.AuthorId);
            return Result<Guid>.Failed(ErrorType.NotFound, "Author Not Found");
        }
        var existingBook = await _bookRepo.GetBookByIsbnAsync(book.Isbn);
        if (existingBook != null)
        {
            _logger.LogWarning("Creating Book Failed : Book with Isbn:{BookIsbn} already exists", book.Isbn);
            return Result<Guid>.Failed(ErrorType.Conflict, "Book with this ISBN already exists");
        }

        var newBook = new Book(book.Isbn, book.Title,book.Description,author,book.DatePublished,book.Rating);
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
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (book.DatePublished > today)
        {
            _logger.LogWarning("Update Book Failed : Date Published {DatePublished} Can't be in the future", book.DatePublished);
            return Result.Failed(ErrorType.BadRequest, "Book Date Published Can't be in the future");
        }
        bookToUpdate.UpdateBook(book.Title, book.Description, book.DatePublished, book.Rating);
        await _bookRepo.SaveChangesAsync();
        _logger.LogInformation("Book {BookId} Updated", editedBookId);
        return Result.Success("Book Updated");
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
        await _bookRepo.SaveChangesAsync();
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
        bookToDelete.DeleteBook();
        await _bookRepo.SaveChangesAsync();
        _logger.LogInformation("Book {BookId} Deleted", id);
        return Result.Success("Book Deleted");
    }
    
}