using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using AuthMicroservice.Application.Dtos;
using Microsoft.AspNetCore.Authentication;

namespace AuthMicroservice.Application.Interface
{
    public interface IUserService
    {
        Task<List<UserDto>> GetAllAsync();
        Task<UserDto> GetByIdAsync(int id);
        Task<UserDto> CreateAsync(CreateUserDto createUserDto);
        Task UpdateAsync(int id, UpdateUserDto updateUserDto);
        Task DeleteAsync(int id);

        // Returns access + refresh tokens for normal login (email/password)
        Task<(string AccessToken, string RefreshToken)> LoginAsync(LoginDto loginDto);

        // OTP flow
        Task<(string AccessToken, string RefreshToken)> VerifyOtpAndGenerateJwt(OtpDto dto);

        // OAuth flow (Google) returns JWT for OAuth user + DTO
        Task<(string AccessToken, OAuthUserDto OAuthUser)> HandleGoogleCallbackAsync(AuthenticationTicket ticket);

        // Refresh and logout
        Task<(string AccessToken, string RefreshToken)> RefreshTokenAsync(string refreshToken);
        Task LogoutAsync(string refreshToken);

        // Current user from claims
        Task<UserDto> GetCurrentUserAsync(ClaimsIdentity identity);
        Task<bool> GetTourStatusAsync(int userId);
        Task MarkTourCompletedAsync(int userId);

        Task UpdateUserRoleAsync(int userId, string role);

    }
}
