using System;
using System.ComponentModel.DataAnnotations;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Polly;
using Polly.Retry;

namespace LoadTester
{
    public class Comment
    {
        public int Id { get; set; }
        public string Text { get; set; }
        public string Captcha { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
    }

    class Program
    {
        private static readonly int TotalRequests = 10000;
        private static readonly int ConcurrentRequests = 1000;
        private static readonly string Url = "https://comments-app:8081/api/comments";

        private static readonly HttpClient httpClient = CreateHttpClient();

        private static readonly AsyncRetryPolicy<HttpResponseMessage> retryPolicy = Policy
            .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (response, timespan, retryCount, context) =>
                {
                    Console.WriteLine($"Retry {retryCount} for {context.PolicyKey} at {context.OperationKey}, waiting {timespan}.");
                });

        static async Task Main(string[] args)
        {
            Console.WriteLine("Starting Load Tester...");

                Console.WriteLine("Load test started...");

                int successful = 0;
                int failed = 0;

                var semaphore = new SemaphoreSlim(ConcurrentRequests);
                var tasks = new Task[TotalRequests];

                var startTime = DateTime.UtcNow;

                for (int i = 0; i < TotalRequests; i++)
                {
                    await semaphore.WaitAsync();

                    int requestId = i;

                    tasks[i] = Task.Run(async () =>
                    {
                        try
                        {
                            var comment = new Comment
                            {
                                Text = new string('A', 500),
                                Captcha = new string('B', 10),
                                UserName = new string('C', 20),
                                Email = $"test{i}@mail.com"
                            };

                            var json = JsonSerializer.Serialize(comment);
                            var content = new StringContent(json, Encoding.UTF8, "application/json");

                            var response = await retryPolicy.ExecuteAsync(
                                async (context) => await httpClient.PostAsync(Url, content),
                                new Context($"Request-{requestId}")
                            );

                            if (response.IsSuccessStatusCode)
                            {
                                Interlocked.Increment(ref successful);
                            }
                            else
                            {
                                Interlocked.Increment(ref failed);
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Request {requestId} failed with exception: {ex.Message}");
                            Interlocked.Increment(ref failed);
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    });

                    if (i % 10 == 0 && i > 0)
                    {
                        Console.WriteLine($"{i} requests queued...");
                    }
                }

                await Task.WhenAll(tasks);

                var elapsed = DateTime.UtcNow - startTime;

                Console.WriteLine("Load test finished.");
                Console.WriteLine($"Total requests: {TotalRequests}");
                Console.WriteLine($"Successful requests: {successful}");
                Console.WriteLine($"Failed requests: {failed}");
                Console.WriteLine($"Time taken: {elapsed.TotalSeconds} seconds");
          
        }

        private static HttpClient CreateHttpClient()
        {
            var handler = new HttpClientHandler
            {
                MaxConnectionsPerServer = 10000,             
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };

            var client = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromMinutes(5)
            };

            return client;
        }
    }
}
