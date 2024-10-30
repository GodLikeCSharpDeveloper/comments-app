using AutoMapper;

namespace CommentApp.Common.AutoMapper
{
    public class AutoMapperService(IMapper mapper) : IAutoMapperService
    {
        private readonly IMapper mapper = mapper;
        public Output Map<Input, Output>(Input input)
        {
            return mapper.Map<Output>(input);
        }
    }
}
