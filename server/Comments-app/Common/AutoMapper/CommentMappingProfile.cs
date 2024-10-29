using AutoMapper;
using CommentApp.Common.Models;
using CommentApp.Common.Models.DTOs;

public class CommentMappingProfile : Profile
{
    public CommentMappingProfile()
    {
        CreateMap<CreateCommentDto, Comment>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.User, opt => opt.MapFrom(src => new User
            {
                UserName = src.UserName,
                Email = src.Email,
                HomePage = src.HomePage
            }))
            .ForMember(dest => dest.UserId, opt => opt.Ignore())
            .ForMember(dest => dest.ParentComment, opt => opt.Ignore())
            .ForMember(dest => dest.Replies, opt => opt.Ignore())
            .ForMember(dest => dest.ImageUrl, opt => opt.Ignore())
            .ForMember(dest => dest.TextFileUrl, opt => opt.Ignore());
    }
}
