namespace CommentApp.Common.Kafka.Consumer
{
    public interface ICommentConsumer
    {
        Task StartConsumingAsync();
        void StopConsuming();
    }
}
