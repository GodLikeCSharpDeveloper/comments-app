using AutoMapper;
using CommentApp.Common.Models;
using CommentApp.Common.Models.DTOs;

namespace CommentApp.Common.AutoMapper
{
    public class CommentMappingProfile : Profile
    {
        public CommentMappingProfile()
        {
            CreateMap<CommentDto, Comment>();
        }
    }
}
