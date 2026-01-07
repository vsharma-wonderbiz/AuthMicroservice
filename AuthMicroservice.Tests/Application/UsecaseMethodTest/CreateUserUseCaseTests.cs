using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AuthMicroservice.Application.Dtos;
using AuthMicroservice.Application.UseCases;
using AuthMicroservice.Domain.Entities;
using AuthMicroservice.Domain.Interface;
using AutoMapper;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace AuthMicroservice.Tests.Application.UsecaseMethodTest
{
    public class CreateUserUseCaseTests
    {
        private readonly Mock<IUserRepository> _mockUserRepository;
        private readonly Mock<IMapper> _mockMapper;
        private readonly UserService _userService;
        private readonly Mock<IOAuthUserRepository> _mockOauth;
        private readonly Mock<IConfiguration> _mockConfiguration;



        public CreateUserUseCaseTests()
        {
            _mockUserRepository = new Mock<IUserRepository>();
            _mockMapper = new Mock<IMapper>();
            _mockConfiguration= new Mock<IConfiguration>();
            _mockOauth = new Mock<IOAuthUserRepository>();
            _userService = new UserService(_mockUserRepository.Object,_mockOauth.Object, _mockMapper.Object,_mockConfiguration.Object);
           
        }

        [Fact]
        public async Task CreateUser_ValidInput_UserIsCreated()
        {
            // Arrange
            var dto = new CreateUserDto
            {
                Username = "vinay",
                Email = "vinay@test.com",
                Password = "Password123!",
                Role = "User"
            };

            var userEntity = new User();
            var userDto = new UserDto
            {
                Username = dto.Username,
                Email = dto.Email,
                Role = dto.Role
            };

            _mockMapper.Setup(x => x.Map<User>(dto)).Returns(userEntity);
            _mockMapper.Setup(x => x.Map<UserDto>(userEntity)).Returns(userDto);

            _mockUserRepository.Setup(x => x.UsernameExistsAsync(dto.Username, null))
                               .ReturnsAsync(false);
            _mockUserRepository.Setup(x => x.EmailExistsAsync(dto.Email, null))
                               .ReturnsAsync(false);
            _mockUserRepository.Setup(x => x.AddAsync(userEntity))
                               .Returns(Task.CompletedTask);

            // Act
            var result = await _userService.CreateAsync(dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(dto.Username, result.Username);
            Assert.Equal(dto.Email, result.Email);

            _mockUserRepository.Verify(
                r => r.AddAsync(It.IsAny<User>()),
                Times.Once
            );
        }
    }
}
