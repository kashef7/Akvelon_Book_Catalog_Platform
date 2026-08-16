using App_BLL.Dtos.BooksDtos;
using App_DAL.Entities;
using AutoMapper;

namespace App_BLL.Mapper.AutoMapper;

public class DomainProfile : Profile
{
    public DomainProfile()
    {
        CreateMap<Book,BookCreateDto>().ReverseMap();
        CreateMap<Book,BookEditDto>().ReverseMap();
        CreateMap<Book,BookGetDto>().ReverseMap();
        CreateMap<BookStatus,BookStatusDto>().ReverseMap();
    }    
}