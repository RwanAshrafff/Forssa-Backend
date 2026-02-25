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
        CreateMap<User, UserDTO>().ReverseMap();
        CreateMap<SuccessStory, SuccessStoryDTO>()
            .ForMember(d => d.UserFullName, o => o.MapFrom(s => s.User.FullName))
            .ForMember(d => d.UserAvatarUrl, o => o.MapFrom(s => s.User.AvatarUrl))
            .ForMember(d => d.CourseTitle, o => o.MapFrom(s => s.Course != null ? s.Course.Title : null));
        CreateMap<CreateSuccessStoryDTO, SuccessStory>();
        
        // Add more mappings as needed
    }
}
