using AutoMapper;
using CommentApp.Common.AutoMapper;
using CommentApp.Common.Models;
using CommentApp.Common.Models.DTOs;
using Microsoft.AspNetCore.Http;
using Moq;

namespace CommentsAppTests.Common.AutoMapper
{
    [TestFixture]
    public class AutoMapperTests
    {
        private AutoMapperService _mapper;
        [SetUp]
        public void Setup()
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<CommentMappingProfile>();
            });

            config.AssertConfigurationIsValid();

            _mapper = new AutoMapperService(config.CreateMapper());
        }
        [Test]
        public void AssertValidMapping()
        {
            //Arrange
            IFormFile imageFile = new Mock<IFormFile>().Object;
            var input = new CreateCommentDto
            {
                Text = "Valid text",
                UserName = "ValidUser",
                Email = "user@example.com",
                Image = imageFile

            };
            var output = new Comment
            {
                Text = input.Text,
                User = new User
                {
                    UserName = input.UserName,
                    Email = input.Email
                },
            };

            //Act
            var testOutput = _mapper.Map<CreateCommentDto, Comment>(input);

            //Assert
            Assert.Multiple(() =>
            {
                Assert.That(input.Text, Is.EqualTo(output.Text));
                Assert.That(string.IsNullOrEmpty(output.ImageUrl), Is.True);
                Assert.That(input.UserName, Is.EqualTo(output.User.UserName));
                Assert.That(input.Email, Is.EqualTo(output.User.Email));
            });
        }
    }
}
