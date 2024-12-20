﻿using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using CommentApp.Common.AutoMapper;
using CommentApp.Common.Data;
using CommentApp.Common.Kafka.Consumer;
using CommentApp.Common.Kafka.Producer;
using CommentApp.Common.Kafka.TopicCreator;
using CommentApp.Common.Models.Options;
using CommentApp.Common.Redis;
using CommentApp.Common.Repositories.CommentRepository;
using CommentApp.Common.Repositories.UserRepository;
using CommentApp.Common.Services.CaptchaService;
using CommentApp.Common.Services.CommentService;
using CommentApp.Common.Services.FileService;
using CommentApp.Common.Services.FileService.FileProcessingService;
using CommentApp.Common.Services.UserService;
using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace CommentApp.Common.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddRepositories(this IServiceCollection services)
        {
            services.AddScoped<ICommentRepository, CommentRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
            return services;
        }

        public static IServiceCollection AddServices(this IServiceCollection services)
        {
            services.AddScoped<ICommentService, CommentService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IFileService, AmazonS3FileService>();
            services.AddScoped<IFileProcessingService, FileProcessingService>();
            services.AddScoped<ICaptchaService, CaptchaService>();
            services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
            services.AddSingleton<IKafkaQueueService, KafkaQueueService>();
            services.AddSingleton<IAutoMapperService, AutoMapperService>();
            services.AddSingleton<IRedisUserCacheService, RedisUserCacheService>();
            services.AddSingleton<RedisDbCacheInitializeService>();
            services.AddSingleton<RedisCleanupService>();
            return services;
        }
        public static async Task<IServiceCollection> InitializeCache(this IServiceCollection services)
        {
            using (var serviceProvider = services.BuildServiceProvider())
            {
                var cacheService = serviceProvider.GetRequiredService<RedisDbCacheInitializeService>();
                var cleanupService = serviceProvider.GetRequiredService<RedisCleanupService>();
                await cleanupService.StopAsync();
                await cacheService.InitializeCache();
            }
            return services;
        }
        public static IServiceCollection AddRedis(this IServiceCollection services, IConfiguration configuration)
        {
            var redisOptions = configuration.GetSection("Redis").Get<RedisOptions>();
            var options = ConfigurationOptions.Parse(redisOptions.ConnectionString);
            options.SyncTimeout = redisOptions.SyncTimeout;
            options.ConnectTimeout = redisOptions.ConnectTimeout;
            options.KeepAlive = redisOptions.KeepAlive;
            options.AbortOnConnectFail = redisOptions.AbortOnConnectFail;
            options.ReconnectRetryPolicy = new ExponentialRetry(redisOptions.ReconnectRetryInterval);
            options.AllowAdmin = redisOptions.AllowAdmin;
            options.DefaultDatabase = redisOptions.DefaultDatabase;

            var redis = ConnectionMultiplexer.Connect(options);
            services.AddSingleton<IConnectionMultiplexer>(redis);
            services.AddSingleton(sp => sp.GetRequiredService<IConnectionMultiplexer>().GetDatabase());

            return services;
        }
        public static IServiceCollection AddAmazonS3(this IServiceCollection services, IConfiguration configuration)
        {
            var awsOptions = configuration.GetSection("S3AWS").Get<S3AWSOptions>();
            services.AddSingleton<IAmazonS3>(provider =>
            {
                var awsCredentials = new BasicAWSCredentials(awsOptions.AccessKey, awsOptions.SecretKey);
                var config = new AmazonS3Config
                {
                    RegionEndpoint = RegionEndpoint.GetBySystemName(awsOptions.Region)
                };
                return new AmazonS3Client(awsCredentials, config);
            });
            return services;
        }

        public static IServiceCollection AddKafka(this IServiceCollection services, IConfiguration configuration)
        {
            var kafkaOptions = configuration.GetSection("Kafka").Get<KafkaOptions>();
            var bootstrapServers = kafkaOptions.BootstrapServers;

            var consumerConfig = new ConsumerConfig
            {
                BootstrapServers = bootstrapServers,
                GroupId = kafkaOptions.GroupId,
                AutoOffsetReset = Enum.TryParse<AutoOffsetReset>(kafkaOptions.AutoOffsetReset, out var offsetReset) ? offsetReset : AutoOffsetReset.Earliest,
                FetchMinBytes = kafkaOptions.FetchMinBytes
            };

            services.AddSingleton(provider =>
            {
                return new ConsumerBuilder<Null, string>(consumerConfig).Build();
            });
            var topicOptions = configuration.GetSection("Topics").Get<List<TopicOptions>>();
            services.AddSingleton<IKafkaTopicCreator>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<KafkaTopicCreator>>();
                return new KafkaTopicCreator(bootstrapServers, logger, topicOptions);
            });
            var producerConfig = new ProducerConfig
            {
                BootstrapServers = bootstrapServers,
                LingerMs = kafkaOptions.Producer.LingerMs,
                BatchSize = kafkaOptions.Producer.BatchSize,
                Acks = kafkaOptions.Producer.Acks switch
                {
                    "None" => Acks.None,
                    "Leader" => Acks.Leader,
                    "All" => Acks.All,
                    _ => Acks.Leader
                }
            };

            services.AddSingleton(provider =>
            {
                return new ProducerBuilder<Null, string>(producerConfig).Build();
            });

            return services;
        }

        public static IServiceCollection AddCustomLogging(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddLogging(configure =>
            {
                configure.AddConsole();
                configure.SetMinimumLevel(Enum.TryParse<LogLevel>(configuration["Logging:LogLevel:Default"], out var level) ? level : LogLevel.Error);
            });

            return services;
        }

        public static IServiceCollection AddCustomHttpClient(this IServiceCollection services, IConfiguration configuration)
        {
            var httpClientOptions = configuration.GetSection("HttpClient:ClientName").Get<HttpClientOptions>();

            services.AddHttpClient("ClientName", client =>
            {
                client.BaseAddress = new Uri(httpClientOptions.BaseAddress);
            })
            .ConfigurePrimaryHttpMessageHandler(() =>
                new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (httpRequestMessage, cert, chain, sslPolicyErrors) =>
                    {
                        return sslPolicyErrors == System.Net.Security.SslPolicyErrors.RemoteCertificateNameMismatch ||
                               sslPolicyErrors == System.Net.Security.SslPolicyErrors.RemoteCertificateChainErrors
                               ? true : sslPolicyErrors == System.Net.Security.SslPolicyErrors.None;
                    },
                    MaxConnectionsPerServer = httpClientOptions.MaxConnectionsPerServer,
                });
            services.AddCors(options =>
            {
                options.AddDefaultPolicy(builder =>
                {
                    builder.AllowAnyOrigin()
                           .AllowAnyMethod()
                           .AllowAnyHeader();
                });
            });
            return services;
        }

        public static IServiceCollection AddBackgroundServices(this IServiceCollection services)
        {
            services.AddHostedService<CommentConsumer>();
            services.AddHostedService<KafkaHostedService>();
            services.AddHostedService<QueuedHostedService>();
            services.AddHostedService<RedisToDbBackgroundService>();
            return services;
        }

        public static IServiceCollection ConfigureDatabase(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<CommentsAppDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"), sqlOptions =>
                    sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 4,
                    maxRetryDelay: TimeSpan.FromSeconds(2),
                    errorNumbersToAdd: null)));
            return services;
        }

        public static IServiceCollection ConfigureKestrelServer(this IServiceCollection services, IConfiguration configuration, IWebHostBuilder webHostBuilder)
        {
            var kestrelLimits = configuration.GetSection("Kestrel:Limits");

            webHostBuilder.ConfigureKestrel(serverOptions =>
            {
                serverOptions.Limits.MaxConcurrentConnections = kestrelLimits.GetValue<int>("MaxConcurrentConnections");
                serverOptions.Limits.MaxConcurrentUpgradedConnections = kestrelLimits.GetValue<int>("MaxConcurrentUpgradedConnections");
                serverOptions.Limits.KeepAliveTimeout = TimeSpan.Parse(kestrelLimits.GetValue<string>("KeepAliveTimeout"));
                serverOptions.Limits.RequestHeadersTimeout = TimeSpan.Parse(kestrelLimits.GetValue<string>("RequestHeadersTimeout"));
            });

            return services;
        }
        public static IServiceCollection AddBackgroundServiceOptions(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<BackgroundRedisOptions>(configuration.GetSection("BackgroundRedisOptions"));
            services.Configure<ConsumerOptions>(configuration.GetSection("KafkaCommentConsumer"));
            return services;
        }
    }
}
