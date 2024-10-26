namespace CommentApp.Common.Models.Options
{
    public class KafkaOptions
    {
        public string BootstrapServers { get; set; }
        public string GroupId { get; set; }
        public string AutoOffsetReset { get; set; }
        public int FetchMinBytes { get; set; }
        public ProducerOptions Producer { get; set; }
    }

    public class ProducerOptions
    {
        public int LingerMs { get; set; }
        public int BatchSize { get; set; }
        public string Acks { get; set; }
    }
}
