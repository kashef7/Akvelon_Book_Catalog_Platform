using App_BLL.Common.Result;
using App_BLL.Dtos.BooksDtos;
using App_BLL.Services.Abstraction;
using App_DAL.Entities;
using App_DAL.Repos.Abstraction;
using AutoMapper;

namespace App_BLL.Services.Implementation;

public class BookService : IBookService
{
    private readonly IBookRepo _bookRepo;
    private readonly IMapper _mapper;
    public BookService(IBookRepo repo, IMapper mapper)
    {
        _bookRepo = repo;
        _mapper = mapper;
        
    }
    public async Task<Result<IReadOnlyList<BookGetDto>>> GetAllBooksAsync()
    {
        var books = await _bookRepo.GetAllBooksAsync();
        var result = _mapper.Map<IReadOnlyList<BookGetDto>>(books);
        return Result<IReadOnlyList<BookGetDto>>.Success(result);
        
    }

    public async Task<Result<BookGetDto>> GetBookAsync(Guid id)
    {
        var book = await _bookRepo.GetBookByIdAsync(id);
        if (book is null)
            return Result<BookGetDto>.Failed(404, "Book Not Found");

        return Result<BookGetDto>.Success(_mapper.Map<BookGetDto>(book));
    }

    public async Task<Result<Guid>> AddBookAsync(BookCreateDto book)
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        if (book.DatePublished > today)
        {
            return Result<Guid>.Failed(400, "Book Date Published Can't be in the future");
        }
        var newBook = _mapper.Map<Book>(book);
        await _bookRepo.AddBookAsync(newBook);
        return Result<Guid>.Success(newBook.Id);
    }

    public async Task<Result> UpdateBookAsync(BookEditDto book, Guid editedBookId)
    {
        var bookToUpdate = await _bookRepo.GetBookByIdAsync(editedBookId);
        if (bookToUpdate == null)
        {
            return Result.Failed(404, "Book Not Found");
        }else if (bookToUpdate.IsDeleted)
        {
            return Result.Failed(404, "Book is Deleted");
        }
        var today = DateOnly.FromDateTime(DateTime.Now);
        if (book.DatePublished > today)
        {
            return Result.Failed(400, "Book Date Published Can't be in the future");
        }
        bookToUpdate.UpdateBook(book.Title,book.Description,book.AuthorName,book.DatePublished,book.Rating,book.Status);
        return Result.Success(204, "Book Updated");
    }

    public async Task<Result> UpdateBookStatusAsync(Guid id, BookStatusDto status)
    {
        var bookToUpdate = await _bookRepo.GetBookByIdAsync(id);
        if (bookToUpdate == null)
        {
            return Result.Failed(404, "Book Not Found");
        }else if (bookToUpdate.IsDeleted)
        {
            return Result.Failed(404, "Book is Deleted");
        }
        var bookToUpdateStatus = _mapper.Map<BookStatus>(status);
        bookToUpdate.UpdateStatus(bookToUpdateStatus);
        return Result.Success(204, "Book Status Updated");
    }

    public async Task<Result> UpdateBookRatingAsync(Guid id, decimal rating)
    {
        var bookToUpdate = await _bookRepo.GetBookByIdAsync(id);
        if (bookToUpdate == null)
        {
            return Result.Failed(404, "Book Not Found");
        }else if (bookToUpdate.IsDeleted)
        {
            return Result.Failed(404, "Book is Deleted");
        }
        bookToUpdate.UpdateRating(rating);
        return Result.Success(204, "Book Ratings Updated");
    }

    public async Task<Result> DeleteBookAsync(Guid id)
    {
        var bookToDelete = await _bookRepo.GetBookByIdAsync(id);
        if (bookToDelete == null)
        {
            return Result.Failed(404, "Book Not Found");
        }else if (bookToDelete.IsDeleted)
        {
            return Result.Failed(404, "Book is already Deleted");
        } 
        await _bookRepo.DeleteBookAsync(bookToDelete.Id);
        return Result.Success(204, "Book Deleted");
    }
}