using System;
using System.Threading.Tasks;
using AuthMicroservice.Application.UseCases;
using AuthMicroservice.Application.Dtos;
using AuthMicroservice.Domain.Entities;
using AuthMicroservice.Domain.Interface;
using Moq;
using Xunit;
using FluentAssertions;

namespace AuthMicroservice.Tests.Application.UsecaseMethodTest
{
    public class UpdateUserUserCaseTest
    {
        private readonly Mock<IUserRepository> _mockUserRepository;
        private readonly UserService _userService;

        public UpdateUserUserCaseTest()
        {
            _mockUserRepository = new Mock<IUserRepository>();

            // Pass null for dependencies not needed for UpdateAsync
            _userService = new UserService(
                _mockUserRepository.Object,
                null,
                null,
                null
            );
        }

        // -------------------- NEGATIVE TESTS --------------------

        [Fact]
        public async Task UpdateAsync_ShouldThrowException_WhenUserNotFound()
        {
            int userId = 1;
            var dto = new UpdateUserDto { Username = "test", Email = "test@test.com" };

            _mockUserRepository.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync((User)null);

            Func<Task> act = async () => await _userService.UpdateAsync(userId, dto);

            await act.Should().ThrowAsync<Exception>()
                .WithMessage($"User with ID {userId} not found.");
        }

        [Fact]
        public async Task UpdateAsync_ShouldThrowException_WhenUsernameAlreadyExists()
        {
            int userId = 1;
            var user = new User { UserId = userId };
            var dto = new UpdateUserDto { Username = "existing", Email = "new@test.com" };

            _mockUserRepository.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);
            _mockUserRepository.Setup(x => x.UsernameExistsAsync(dto.Username, userId)).ReturnsAsync(true);

            Func<Task> act = async () => await _userService.UpdateAsync(userId, dto);

            await act.Should().ThrowAsync<Exception>()
                .WithMessage($"Username '{dto.Username}' is already taken.");
        }

        [Fact]
        public async Task UpdateAsync_ShouldThrowException_WhenEmailAlreadyExists()
        {
            int userId = 1;
            var user = new User { UserId = userId };
            var dto = new UpdateUserDto { Username = "newuser", Email = "existing@test.com" };

            _mockUserRepository.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);
            _mockUserRepository.Setup(x => x.UsernameExistsAsync(dto.Username, userId)).ReturnsAsync(false);
            _mockUserRepository.Setup(x => x.EmailExistsAsync(dto.Email, userId)).ReturnsAsync(true);

            Func<Task> act = async () => await _userService.UpdateAsync(userId, dto);

            await act.Should().ThrowAsync<Exception>()
                .WithMessage($"Email '{dto.Email}' is already registered.");
        }

    

        [Fact]
        public async Task UpdateAsync_ShouldUpdateUser_WhenValidInput_WithPassword()
        {
            int userId = 1;
            var user = new User { UserId = userId, Username = "old", Email = "old@test.com", PasswordHash = "oldhash", Role = "User" };
            var dto = new UpdateUserDto { Username = "newuser", Email = "new@test.com", Password = "newpass", Role = "Admin" };

            _mockUserRepository.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);
            _mockUserRepository.Setup(x => x.UsernameExistsAsync(dto.Username, userId)).ReturnsAsync(false);
            _mockUserRepository.Setup(x => x.EmailExistsAsync(dto.Email, userId)).ReturnsAsync(false);
            _mockUserRepository.Setup(x => x.UpdateAsync(user)).Returns(Task.CompletedTask);

            await _userService.UpdateAsync(userId, dto);

            user.Username.Should().Be(dto.Username);
            user.Email.Should().Be(dto.Email);
            BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash).Should().BeTrue();
            user.Role.Should().Be(dto.Role);

            _mockUserRepository.Verify(x => x.UpdateAsync(user), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_ShouldUpdateUser_WhenValidInput_WithoutPassword()
        {
            int userId = 1;
            var oldPasswordHash = BCrypt.Net.BCrypt.HashPassword("oldpass");
            var user = new User { UserId = userId, Username = "old", Email = "old@test.com", PasswordHash = oldPasswordHash, Role = "User" };
            var dto = new UpdateUserDto { Username = "newuser", Email = "new@test.com", Password = null, Role = "Admin" };

            _mockUserRepository.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);
            _mockUserRepository.Setup(x => x.UsernameExistsAsync(dto.Username, userId)).ReturnsAsync(false);
            _mockUserRepository.Setup(x => x.EmailExistsAsync(dto.Email, userId)).ReturnsAsync(false);
            _mockUserRepository.Setup(x => x.UpdateAsync(user)).Returns(Task.CompletedTask);

            await _userService.UpdateAsync(userId, dto);

            user.Username.Should().Be(dto.Username);
            user.Email.Should().Be(dto.Email);
            user.PasswordHash.Should().Be(oldPasswordHash); // old password remains
            user.Role.Should().Be(dto.Role);

            _mockUserRepository.Verify(x => x.UpdateAsync(user), Times.Once);
        }
    }
}
