using CommentApp.Common.Data;
using CommentApp.Common.Kafka.Consumer;
using CommentApp.Common.Kafka.TopicCreator;
using CommentApp.Common.Repositories.CommentRepository;
using CommentApp.Common.Repositories.UserRepository;
using CommentApp.Common.Services.CommentService;
using CommentApp.Common.Services.UserService;
using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddLogging(configure =>
{
    configure.AddConsole();
    configure.SetMinimumLevel(LogLevel.Error);
});
builder.Services.AddDbContext<CommentsAppDbContext>(options =>
            options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<ICommentRepository, CommentRepository>();
builder.Services.AddScoped<ICommentService, CommentService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserService, UserService>();
var bootstrapServers = builder.Configuration.GetSection("Kafka")["BootstrapServers"];
builder.Services.AddSingleton(provider =>
{
    var config = new ConsumerConfig
    {
        BootstrapServers = bootstrapServers,
        GroupId = "comment-consumers",
        AutoOffsetReset = AutoOffsetReset.Earliest
    };
    return new ConsumerBuilder<Null, string>(config).Build();
});

builder.Services.AddSingleton<IKafkaTopicCreator>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<KafkaTopicCreator>>();
    return new KafkaTopicCreator(bootstrapServers, logger);
});
builder.Services.AddSingleton<IProducer<Null, string>>(provider =>
{
    var config = new ProducerConfig
    {
        BootstrapServers = bootstrapServers
    };
    return new ProducerBuilder<Null, string>(config).Build();
});
builder.Services.AddHttpClient("ClientName", client =>
{
    client.BaseAddress = new Uri("https://comments-app:8081");
})
.ConfigurePrimaryHttpMessageHandler(() =>
    new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = (httpRequestMessage, cert, chain, sslPolicyErrors) =>
        {
            // Отключаем проверку имени сертификата
            return sslPolicyErrors == System.Net.Security.SslPolicyErrors.RemoteCertificateNameMismatch ||
                   sslPolicyErrors == System.Net.Security.SslPolicyErrors.RemoteCertificateChainErrors
                   ? true : sslPolicyErrors == System.Net.Security.SslPolicyErrors.None;
        }
    });

builder.Services.AddSingleton<ICommentConsumer, CommentConsumer>();
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<CommentsAppDbContext>();
    dbContext.Database.Migrate();
}
using (var scope = app.Services.CreateScope())
{
    var consumer = scope.ServiceProvider.GetRequiredService<ICommentConsumer>();
    await consumer.StartConsumingAsync();
}
app.MapControllers();
app.Run();

