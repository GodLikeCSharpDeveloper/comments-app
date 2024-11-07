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
         .ForMember(dest => dest.ParentCommentId, opt => opt.MapFrom(src => ParseParentCommentId(src.ParentCommentId)))
         .ForMember(dest => dest.UserId, opt => opt.Ignore())
         .ForMember(dest => dest.ParentComment, opt => opt.Ignore())
         .ForMember(dest => dest.Replies, opt => opt.Ignore())
         .ForMember(dest => dest.ImageUrl, opt => opt.Ignore())
         .ForMember(dest => dest.TextFileUrl, opt => opt.Ignore());
    }
    private static int? ParseParentCommentId(string parentCommentId)
    {
        return int.TryParse(parentCommentId, out int id) ? (int?)id : null;
    }
}
