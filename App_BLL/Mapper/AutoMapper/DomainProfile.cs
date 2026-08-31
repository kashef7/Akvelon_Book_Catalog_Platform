using App_BLL.Dtos.AuthorsDtos;
using App_BLL.Dtos.BooksDtos;
using App_BLL.Dtos.LoansDtos;
using App_BLL.Dtos.UsersDtos;
using App_BLL.QueryParams.Author;
using App_BLL.QueryParams.Book;
using App_BLL.QueryParams.Loan;
using App_BLL.QueryParams.User;
using App_Common.Common.Author;
using App_Common.Common.Book;
using App_Common.Common.Loan;
using App_Common.Common.User;
using App_DAL.Entities.Authors;
using App_DAL.Entities.Books;
using App_DAL.Entities.Loans;
using App_DAL.Entities.Users;
using AutoMapper;

namespace App_BLL.Mapper.AutoMapper;

public class DomainProfile : Profile
{
    public DomainProfile()
    {
        CreateMap<Book,BookGetDto>().ForMember(d=> d.AuthorName, opt => opt.MapFrom(s => s.Author.Name)).ReverseMap();
        CreateMap<BookQueryParams,BookQuery>().ReverseMap();
        
        
        CreateMap<AuthorCreateDto, Author>();
        CreateMap<AuthorEditDto, Author>();
        CreateMap<Author,AuthorGetDto>().ReverseMap();
        CreateMap<AuthorQueryParams,AuthorQuery>().ReverseMap();

        CreateMap<UserCreateDto, User>();
        CreateMap<UserEditDto, User>();
        CreateMap<User, UserGetDto>().ReverseMap();
        CreateMap<UserQueryParams, UserQuery>().ReverseMap();
        
        CreateMap<LoanQueryParams, LoanQuery>().ReverseMap();
        CreateMap<Loan,LoanGetDto>()
            .ForMember(l => l.BookTitle, opt => opt.MapFrom(l => l.Book.Title))
            .ForMember(l => l.UserName, opt => opt.MapFrom(l => l.User.Name));
    }    
}