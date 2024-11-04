using NUnit.Framework;
using Moq;
using System.Threading.Tasks;
using System.Threading;
using System;
using Confluent.Kafka;
using System.Threading.Channels;

namespace CommentsAppTests.Common.Kafka.Producer
{
    [TestFixture]
    public class KafkaQueueServiceTests
    {
        private KafkaQueueService _service;

        [SetUp]
        public void Setup()
        {
            _service = new KafkaQueueService();
        }

        [Test]
        public async Task EnqueueMessageAsync_ShouldEnqueueMessageSuccessfully()
        {
            // Arrange
            var message = new Message<Null, string> { Value = "Test Message" };

            // Act
            await _service.EnqueueMessageAsync(message);

            // Assert
            var readMessage = await _service.MessageChannel.Reader.ReadAsync();
            Assert.That(readMessage.Value, Is.EqualTo(message.Value), "Сообщение не было корректно добавлено в канал.");
        }

        [Test]
        public void EnqueueMessageAsync_WithCanceledToken_ShouldThrowOperationCanceledException()
        {
            // Arrange
            var message = new Message<Null, string> { Value = "Test Message" };
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            Assert.ThrowsAsync<TaskCanceledException>(async () =>
                await _service.EnqueueMessageAsync(message, cts.Token),
                "Exception expected OperationCanceledException after cancellationToken cancel.");
        }

        [Test]
        public async Task EnqueueMessageAsync_WithMultipleWriters_ShouldEnqueueAllMessages()
        {
            // Arrange
            var messages = new[]
            {
                new Message<Null, string> { Value = "Message 1" },
                new Message<Null, string> { Value = "Message 2" },
                new Message<Null, string> { Value = "Message 3" }
            };

            // Act
            var enqueueTasks = new Task[messages.Length];
            for (int i = 0; i < messages.Length; i++)
            {
                enqueueTasks[i] = _service.EnqueueMessageAsync(messages[i]);
            }

            await Task.WhenAll(enqueueTasks);

            // Assert
            foreach (var expectedMessage in messages)
            {
                var readMessage = await _service.MessageChannel.Reader.ReadAsync();
                Assert.That(readMessage.Value, Is.EqualTo(expectedMessage.Value), $"Сообщение '{expectedMessage.Value}' не было корректно добавлено в канал.");
            }
        }
    }
}
