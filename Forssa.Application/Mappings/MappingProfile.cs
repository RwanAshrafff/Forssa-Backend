using AutoMapper;
using Forssa.Application.DTOs;
using Forssa.Domain.Entities;

namespace Forssa.Application.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // Add your entity to DTO mappings here
        CreateMap<Product, ProductDTO>().ReverseMap();
        
        // Add more mappings as needed
    }
}
