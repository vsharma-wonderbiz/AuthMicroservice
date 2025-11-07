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
        //Task<(string AccessToken, string RefreshToken)> LoginAsync(LoginDto loginDto);

        Task<(string AccessToken, string RefreshToken)> VerifyOtpAndGenerateJwt(OtpDto dto);
        Task<string> LoginAsync(LoginDto loginDto);
        Task<(string AccessToken, OAuthUserDto OAuthUser)> HandleGoogleCallbackAsync(AuthenticationTicket ticket);
        Task<(string AccessToken, string RefreshToken)> RefreshTokenAsync(string refreshToken);
        Task LogoutAsync(string refreshToken);
        Task<UserDto> GetCurrentUserAsync(ClaimsIdentity identity);
    }
}
