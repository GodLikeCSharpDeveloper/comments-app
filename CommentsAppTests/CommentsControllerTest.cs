using NUnit.Framework;
using System.Threading.Tasks;

[TestFixture]
public class CommentsControllerTests
{
    private Mock<ILogger<CommentsController>> _loggerMock;
    private Mock<IMapper> _mapperMock;
    private Mock<IKafkaQueueService> _kafkaMock;
    private CommentsController _controller;

    [SetUp]
    public void Setup()
    {
        _loggerMock = new Mock<ILogger<CommentsController>>();
        _mapperMock = new Mock<IMapper>();
        _kafkaMock = new Mock<IKafkaQueueService>();
        _controller = new CommentsController(_loggerMock.Object, _mapperMock.Object, _kafkaMock.Object);
    }

    [Test]
    public async Task PostComment_ValidModel_ReturnsOk()
    {
        // Arrange
        var dto = new CreateCommentDto { /* заполните свойства */ };
        var comment = new Comment { /* заполните свойства */ };
        _mapperMock.Setup(m => m.Map<Comment>(dto)).Returns(comment);
        _kafkaMock.Setup(k => k.EnqueueMessageAsync(It.IsAny<Message<Null, string>>()))
                  .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.PostComment(dto);

        // Assert
        Assert.IsInstanceOf<OkResult>(result);
        _kafkaMock.Verify(k => k.EnqueueMessageAsync(It.IsAny<Message<Null, string>>()), Times.Once);
    }
}
