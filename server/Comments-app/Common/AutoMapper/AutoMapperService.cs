using AutoMapper;

namespace CommentApp.Common.AutoMapper
{
    public class AutoMapperService(IMapper mapper) : IAutoMapperService
    {
        private readonly IMapper mapper = mapper;
        public Output? Map<Input, Output>(Input? input)
        {
            return mapper.Map<Output>(input);
        }
        public List<Output> Map<Input, Output>(List<Input> input)
        {
            var outputs = new List<Output>();
            foreach (var item in input)
            {
                outputs.Add(mapper.Map<Output>(item));
            }
            return outputs;
        }
    }
}
