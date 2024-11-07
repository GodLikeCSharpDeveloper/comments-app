namespace CommentApp.Common.AutoMapper
{
    public interface IAutoMapperService
    {
        Output? Map<Input, Output>(Input? input);
        List<Output> Map<Input, Output>(List<Input> input);
    }
}
