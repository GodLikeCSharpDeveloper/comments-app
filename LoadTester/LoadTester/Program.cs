using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CommentApp.Common.Models;
using Newtonsoft.Json;
using Polly;
using Polly.Retry;

namespace LoadTester
{
    class Program
    {
        private static readonly int TotalRequests = 10000;
        private static readonly int ConcurrentRequests = 100;
        private static readonly string Url = "https://comments-app:8081/api/comments";
        static async Task Main(string[] args)
        {
            while (true)
            {
                Console.WriteLine("Load test started...");

                var handler = new HttpClientHandler
                {
                    MaxConnectionsPerServer = ConcurrentRequests,
                    ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true // Отключаем проверку сертификатов
                };

                using var httpClient = new HttpClient(handler);

                AsyncRetryPolicy<HttpResponseMessage> retryPolicy = Policy
                    .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
                    .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

                int successful = 0;
                int failed = 0;

                using var semaphore = new SemaphoreSlim(ConcurrentRequests);

                var tasks = new Task[TotalRequests];

                for (int i = 0; i < TotalRequests; i++)
                {
                    await semaphore.WaitAsync();

                    tasks[i] = Task.Run(async () =>
                    {
                        try
                        {
                            var comment = new Comment() { Text = "test text", Captcha = "text captcha", Id = i };

                            var json = JsonConvert.SerializeObject(comment);
                            var jsonContent = new StringContent(json, Encoding.UTF8, "application/json");
                            var response = await retryPolicy.ExecuteAsync(() => httpClient.PostAsync(Url, jsonContent));

                            if (response.IsSuccessStatusCode)
                            {
                                Interlocked.Increment(ref successful);
                            }
                            else
                            {
                                Interlocked.Increment(ref failed);
                            }
                        }
                        catch (Exception)
                        {
                            Interlocked.Increment(ref failed);
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    });

                    if (i % 100 == 0)
                    {
                        Console.WriteLine($"{i} requests sent...");
                    }
                }

                await Task.WhenAll(tasks);

                Console.WriteLine("Load test finished.");
                Console.WriteLine($"Successful requests: {successful}");
                Console.WriteLine($"Failed requests: {failed}");
                Console.WriteLine();
            }
        }
    }
}
