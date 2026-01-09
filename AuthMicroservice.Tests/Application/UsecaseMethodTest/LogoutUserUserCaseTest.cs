using System.Threading.Tasks;
using AuthMicroservice.Application.UseCases;
using AuthMicroservice.Domain.Entities;
using AuthMicroservice.Domain.Interface;
using Moq;
using Xunit;
using FluentAssertions;

namespace AuthMicroservice.Tests.Application.UsecaseMethodTest
{
    public class LogoutUserUserCaseTest
    {
        private readonly Mock<IUserRepository> _mockUserRepository;
        private readonly Mock<IOAuthUserRepository> _mockOauthRepository;
        private readonly UserService _userService;

        public LogoutUserUserCaseTest()
        {
            _mockUserRepository = new Mock<IUserRepository>();
            _mockOauthRepository = new Mock<IOAuthUserRepository>();

          
            _userService = new UserService(
                _mockUserRepository.Object,
                _mockOauthRepository.Object,
                null,    // Mapper not needed here
                null     // Config not needed
            );
        }

       

        [Fact]
        public async Task LogoutAsync_ShouldDoNothing_WhenRefreshTokenIsNullOrEmpty()
        {
            
            await _userService.LogoutAsync(null);
            await _userService.LogoutAsync("");

           
            _mockUserRepository.Verify(x => x.GetByRefreshTokenAsync(It.IsAny<string>()), Times.Never);
            _mockOauthRepository.Verify(x => x.GetByRefreshTokenAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task LogoutAsync_ShouldClearUserRefreshToken_WhenUserFound()
        {
            
            var refreshToken = "valid-user-refresh-token";
            var user = new User
            {
                RefreshToken = refreshToken
            };

            _mockUserRepository.Setup(x => x.GetByRefreshTokenAsync(refreshToken))
                .ReturnsAsync(user);

            _mockUserRepository.Setup(x => x.UpdateAsync(user))
                .Returns(Task.CompletedTask);

            
            await _userService.LogoutAsync(refreshToken);

          
            user.RefreshToken.Should().BeNull();
            user.RefreshTokenExpiry.Should().BeNull();

            _mockUserRepository.Verify(x => x.UpdateAsync(user), Times.Once);
            _mockOauthRepository.Verify(x => x.GetByRefreshTokenAsync(It.IsAny<string>()), Times.Never);
        }   

        [Fact]
        public async Task LogoutAsync_ShouldClearOAuthUserRefreshToken_WhenUserNotFoundButOAuthUserFound()
        {
            // Arrange
            var refreshToken = "valid-oauth-refresh-token";

            _mockUserRepository.Setup(x => x.GetByRefreshTokenAsync(refreshToken))
                .ReturnsAsync((User)null);

            var oAuthUser = new Domain.Entities.OAuthUser
            {
                RefreshToken = refreshToken
            };

            _mockOauthRepository.Setup(x => x.GetByRefreshTokenAsync(refreshToken))
                .ReturnsAsync(oAuthUser);

            _mockOauthRepository.Setup(x => x.UpdateAsync(oAuthUser))
                .Returns(Task.CompletedTask);

            
            await _userService.LogoutAsync(refreshToken);

           
            oAuthUser.RefreshToken.Should().BeNull();
            _mockOauthRepository.Verify(x => x.UpdateAsync(oAuthUser), Times.Once);
            _mockUserRepository.Verify(x => x.UpdateAsync(It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public async Task LogoutAsync_ShouldDoNothing_WhenTokenNotFound()
        {
            
            var refreshToken = "non-existent-token";

            _mockUserRepository.Setup(x => x.GetByRefreshTokenAsync(refreshToken))
                .ReturnsAsync((User)null);

            _mockOauthRepository.Setup(x => x.GetByRefreshTokenAsync(refreshToken))
                .ReturnsAsync((Domain.Entities.OAuthUser)null);

            
            await _userService.LogoutAsync(refreshToken);

            
            _mockUserRepository.Verify(x => x.UpdateAsync(It.IsAny<User>()), Times.Never);
            _mockOauthRepository.Verify(x => x.UpdateAsync(It.IsAny<Domain.Entities.OAuthUser>()), Times.Never);
        }
    }
}
