using Confluent.Kafka;
using Moq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommentsAppTests
{
    public class BackgroundTaskQueueTests
    {
        private BackgroundTaskQueue _service;
        [SetUp]
        public void Setup()
        {
            _service = new BackgroundTaskQueue();
        }

        [Test]
        public async Task EnqueueAndDequeue_SingleItem_ShouldReturnSameItem()
        {
            // Arrange
            Func<CancellationToken, Task> workItem = (ct) => Task.CompletedTask;

            // Act
            _service.QueueBackgroundWorkItem(workItem);
            var dequeuedItem = await _service.DequeueAsync(CancellationToken.None);

            // Assert
            Assert.That(dequeuedItem, Is.EqualTo(workItem));
        }
        [Test]
        public void EnqueueNullAsync_ShouldThrowArgumentNullException()
        {
            //Assert
            Assert.Throws<ArgumentNullException>(() => _service.QueueBackgroundWorkItem(null));
        }
        [Test]
        public void DequeueTaskAsync_WithCanceledToken_ShouldThrowOperationCanceledException()
        {
            // Arrange
            var message = new Message<Null, string> { Value = "Test Message" };
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            Assert.ThrowsAsync<TaskCanceledException>(async () =>
                await _service.DequeueAsync(cts.Token),
                "Exception expected OperationCanceledException after cancellationToken cancel.");
        }
        [Test]
        public void DequeueAsync_WithCancellationBeforeEnqueue_ShouldThrowOperationCanceledException()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            Assert.ThrowsAsync<TaskCanceledException>(async () =>
                await _service.DequeueAsync(cts.Token));
        }
        [Test]
        public async Task EnqueueMultipleItems_DequeueInOrder_ShouldReturnItemsInSameOrder()
        {
            // Arrange
            Func<CancellationToken, Task> workItem1 = (ct) => Task.CompletedTask;
            Func<CancellationToken, Task> workItem2 = (ct) => Task.CompletedTask;
            Func<CancellationToken, Task> workItem3 = (ct) => Task.CompletedTask;

            // Act
            _service.QueueBackgroundWorkItem(workItem1);
            _service.QueueBackgroundWorkItem(workItem2);
            _service.QueueBackgroundWorkItem(workItem3);

            var dequeued1 = await _service.DequeueAsync(CancellationToken.None);
            var dequeued2 = await _service.DequeueAsync(CancellationToken.None);
            var dequeued3 = await _service.DequeueAsync(CancellationToken.None);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(dequeued1, Is.EqualTo(workItem1));
                Assert.That(dequeued2, Is.EqualTo(workItem2));
                Assert.That(dequeued3, Is.EqualTo(workItem3));
            });
        }
        [Test]
        public async Task EnqueueAndDequeue_ConcurrentOperations_ShouldMaintainIntegrity()
        {
            // Arrange
            int numberOfItems = 100;
            var workItems = new Func<CancellationToken, Task>[numberOfItems];
            for (int i = 0; i < numberOfItems; i++)
            {
                workItems[i] = (ct) => Task.Delay(1, ct);
            }

            // Act
            Parallel.For(0, numberOfItems, i =>
            {
                _service.QueueBackgroundWorkItem(workItems[i]);
            });

            var dequeuedItems = new Func<CancellationToken, Task>[numberOfItems];
            var dequeueTasks = new Task[numberOfItems];
            for (int i = 0; i < numberOfItems; i++)
            {
                int capture = i;
                dequeueTasks[i] = Task.Run(async () =>
                {
                    dequeuedItems[capture] = await _service.DequeueAsync(CancellationToken.None);
                });
            }

            await Task.WhenAll(dequeueTasks);

            // Assert
            for (int i = 0; i < numberOfItems; i++)
            {
                Assert.That(dequeuedItems[i], Is.EqualTo(workItems[i]));
            }
        }

    }
}

