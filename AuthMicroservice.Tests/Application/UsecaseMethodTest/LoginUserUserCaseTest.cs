using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AuthMicroservice.Application.UseCases;
using AuthMicroservice.Domain.Interface;
using AutoMapper;
using Moq;
using Microsoft.Extensions.Configuration;
using AuthMicroservice.Application.Dtos;
using Xunit;
using FluentAssertions;
using AuthMicroservice.Domain.Entities;

namespace AuthMicroservice.Tests.Application.UsecaseMethodTest
{
    public class LoginUserUserCaseTest
    {
        private readonly Mock<IUserRepository> _mockUserRepository;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<IOAuthUserRepository> _mockOauth;
        private readonly UserService _userService;
        private readonly Mock<IConfiguration> _mockConfiguration;

        public LoginUserUserCaseTest()
        {
            _mockUserRepository = new Mock<IUserRepository>();
            _mockMapper = new Mock<IMapper>();
            _mockOauth = new Mock<IOAuthUserRepository>();

            _mockConfiguration = new Mock<IConfiguration>();
            _mockConfiguration.Setup(c => c["Jwt:Key"]).Returns("THIS_IS_A_SUPER_SECRET_TEST_KEY_1234567890_ABC");
            _mockConfiguration.Setup(c => c["Jwt:Issuer"]).Returns("TestIssuer");
            _mockConfiguration.Setup(c => c["Jwt:Audience"]).Returns("TestAudience");
            _mockConfiguration.Setup(c => c["Jwt:AccessTokenExpiryMinutes"]).Returns("60");
            _mockConfiguration.Setup(c => c["Jwt:RefreshTokenExpiryDays"]).Returns("7");


            // ✅ REAL in-memory configuration (NOT mock)


            _userService = new UserService(
                _mockUserRepository.Object,
                _mockOauth.Object,
                _mockMapper.Object, 
               _mockConfiguration.Object

            );
        }

        // -------------------- NEGATIVE TESTS --------------------

        [Fact]
        public async Task LoginUser_ShouldThrowException_WhenEmailOrPasswordIsNull()
        {
            var credentials = new LoginDto
            {
                Email = "",
                Password = ""
            };

            var act = async () => await _userService.LoginAsync(credentials);

            await act.Should()
                .ThrowAsync<Exception>()
                .WithMessage("Email and password are required.");
        }

        [Fact]
        public async Task LoginUser_ShouldThrowException_WhenUserDoesNotExist()
        {
            _mockUserRepository
                .Setup(x => x.GetByEmailAsync(It.IsAny<string>()))
                .ReturnsAsync((User)null);

            var credentials = new LoginDto
            {
                Email = "test@gmail.com",
                Password = "password"
            };

            var act = async () => await _userService.LoginAsync(credentials);

            await act.Should()
                .ThrowAsync<Exception>()
                .WithMessage("Invalid email or password.");
        }

        [Fact]
        public async Task LoginUser_ShouldThrowException_WhenPasswordIsIncorrect()
        {
            var user = new User
            {
                Email = "test@gmail.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("correct-password")
            };

            _mockUserRepository
                .Setup(x => x.GetByEmailAsync(It.IsAny<string>()))
                .ReturnsAsync(user);

            var credentials = new LoginDto
            {
                Email = "test@gmail.com",
                Password = "wrong-password"
            };

            var act = async () => await _userService.LoginAsync(credentials);

            await act.Should()
                .ThrowAsync<Exception>()
                .WithMessage("Invalid email or password.");
        }

        // -------------------- POSITIVE TEST --------------------

        //[Fact]
        //public async Task LoginAsync_ShouldReturnTokens_WhenCredentialsAreValid()
        //{
        //    var user = new User
        //    {
        //        Email = "test@test.com",
        //        PasswordHash = BCrypt.Net.BCrypt.HashPassword("password")
        //    };

        //    _mockUserRepository
        //        .Setup(r => r.GetByEmailAsync(It.IsAny<string>()))
        //        .ReturnsAsync(user);

        //    _mockUserRepository
        //        .Setup(r => r.UpdateAsync(It.IsAny<User>()))
        //        .Returns(Task.CompletedTask);

        //    var loginDto = new LoginDto
        //    {
        //        Email = "test@test.com",
        //        Password = "password"
        //    };

        //    // Act
        //    var result = await _userService.LoginAsync(loginDto);

        //    // Assert (NO JHOL)
        //    result.AccessToken.Should().NotBeNullOrEmpty();
        //    result.RefreshToken.Should().NotBeNullOrEmpty();

        //    _mockUserRepository.Verify(
        //        r => r.UpdateAsync(It.Is<User>(
        //            u => u.RefreshTokenExpiry > DateTime.UtcNow
        //        )),
        //        Times.Once
        //    );
        //}

       


    }
}
