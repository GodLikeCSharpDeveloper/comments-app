using AutoMapper;
using CommentApp.Common.Controllers;
using CommentApp.Common.Kafka.Producer;
using CommentApp.Common.Models.DTOs;
using CommentApp.Common.Models;
using CommentApp.Common.Services.FileService;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using CommentApp.Common.AutoMapper;

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
                Captcha = "ValidCaptcha",
                UserName = "ValidUser",
                Email = "user@example.com",
                Image = imageFile

            };
            var output = new Comment
            {
                Text = input.Text,
                Captcha = input.Captcha,
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
                Assert.That(input.Captcha, Is.EqualTo(output.Captcha));
                Assert.That(string.IsNullOrEmpty(output.ImageUrl), Is.True);
                Assert.That(input.UserName, Is.EqualTo(output.User.UserName));
                Assert.That(input.Email, Is.EqualTo(output.User.Email));
            });
        }
    }
}
