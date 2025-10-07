using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AuthMicroservice.Application.Interface;
using AuthMicroservice.Domain.Interface;
using AuthMicroservice.Domain.Entities;
using Microsoft.Extensions.Configuration;
using AutoMapper;
using AuthMicroservice.Application.Dtos;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

namespace AuthMicroservice.Application.UseCases
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IOAuthUserRepository _oAuthUserRepository;
        private readonly IMapper _mapper;
        private readonly IConfiguration _configuration;

        public UserService(IUserRepository userRepository, IOAuthUserRepository oAuthUserRepository, IMapper mapper, IConfiguration configuration)
        {
            _userRepository = userRepository;
            _oAuthUserRepository = oAuthUserRepository;
            _mapper = mapper;
            _configuration = configuration;
        }

        public async Task<List<UserDto>> GetAllAsync()
        {
            if (_userRepository == null)
                throw new InvalidOperationException("IUserRepository is not injected.");
            var users = await _userRepository.GetAllAsync();
            return _mapper.Map<List<UserDto>>(users);
        }

        public async Task<UserDto> GetByIdAsync(int id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
                throw new Exception($"User with ID {id} not found.");
            return _mapper.Map<UserDto>(user);
        }

        public async Task<UserDto> CreateAsync(CreateUserDto createUserDto)
        {
            if (string.IsNullOrEmpty(createUserDto.Username) || string.IsNullOrEmpty(createUserDto.Email) || string.IsNullOrEmpty(createUserDto.Password))
                throw new Exception("Username, email, and password are required.");

            if (await _userRepository.UsernameExistsAsync(createUserDto.Username))
                throw new Exception($"Username '{createUserDto.Username}' is already taken.");

            if (await _userRepository.EmailExistsAsync(createUserDto.Email))
                throw new Exception($"Email '{createUserDto.Email}' is already registered.");

            var user = _mapper.Map<User>(createUserDto);
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(createUserDto.Password);
            user.Role = createUserDto.Role ?? "User";

            await _userRepository.AddAsync(user);
            return _mapper.Map<UserDto>(user);
        }

        public async Task UpdateAsync(int id, UpdateUserDto updateUserDto)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
                throw new Exception($"User with ID {id} not found.");

            if (await _userRepository.UsernameExistsAsync(updateUserDto.Username, id))
                throw new Exception($"Username '{updateUserDto.Username}' is already taken.");

            if (await _userRepository.EmailExistsAsync(updateUserDto.Email, id))
                throw new Exception($"Email '{updateUserDto.Email}' is already registered.");

            user.Username = updateUserDto.Username;
            user.Email = updateUserDto.Email;
            if (!string.IsNullOrEmpty(updateUserDto.Password))
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(updateUserDto.Password);
            user.Role = updateUserDto.Role ?? user.Role;

            await _userRepository.UpdateAsync(user);
        }

        public async Task DeleteAsync(int id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
                throw new Exception($"User with ID {id} not found.");
            await _userRepository.DeleteAsync(id);
        }

        public async Task<(string AccessToken, string RefreshToken)> LoginAsync(LoginDto loginDto)
        {
            if (string.IsNullOrEmpty(loginDto.Email) || string.IsNullOrEmpty(loginDto.Password))
                throw new Exception("Email and password are required.");

            var user = await _userRepository.GetByEmailAsync(loginDto.Email);
            if (user == null || !BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
                throw new Exception("Invalid email or password.");

            var (accessToken, refreshToken) = GenerateTokens(user);
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
            await _userRepository.UpdateAsync(user);

            return (accessToken, refreshToken);
        }

        public async Task<(string AccessToken, OAuthUserDto OAuthUser)> HandleGoogleCallbackAsync(AuthenticationTicket ticket)
        {
            var email = ticket.Principal.FindFirst(ClaimTypes.Email)?.Value;
            var name = ticket.Principal.FindFirst(ClaimTypes.Name)?.Value;
            var googleId = ticket.Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var accessToken = ticket.Properties.GetTokenValue("access_token");
            var refreshToken = ticket.Properties.GetTokenValue("refresh_token");

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(googleId))
                throw new Exception("Email or Google ID not provided.");

            var existingUser = await _userRepository.GetByEmailAsync(email);
            if (existingUser != null)
                throw new Exception("Email already registered with password login. Please link accounts.");

            var oAuthUser = await _oAuthUserRepository.GetByGoogleIdAsync(googleId);
            if (oAuthUser == null)
            {
                oAuthUser = new OAuthUser
                {
                    Email = email,
                    Username = name ?? email.Split('@')[0],
                    Role = "User",
                    GoogleId = googleId,
                    AccessToken = accessToken,
                    RefreshToken = refreshToken
                };
                await _oAuthUserRepository.AddAsync(oAuthUser);
            }
            else
            {
                oAuthUser.AccessToken = accessToken;
                oAuthUser.RefreshToken = refreshToken;
                await _oAuthUserRepository.UpdateAsync(oAuthUser);
            }

            var jwtToken = GenerateJwtTokenForOAuthUser(oAuthUser);
            return (jwtToken, _mapper.Map<OAuthUserDto>(oAuthUser));
        }

        public async Task<(string AccessToken, string RefreshToken)> RefreshTokenAsync(string refreshToken)
        {
            if (string.IsNullOrEmpty(refreshToken))
                throw new Exception("No refresh token provided.");

            var user = await _userRepository.GetByRefreshTokenAsync(refreshToken);
            if (user != null)
            {
                if (user.RefreshTokenExpiry < DateTime.UtcNow)
                    throw new Exception("Refresh token expired.");

                var (accessToken, newRefreshToken) = GenerateTokens(user);
                user.RefreshToken = newRefreshToken;
                user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
                await _userRepository.UpdateAsync(user);
                return (accessToken, newRefreshToken);
            }

            var oAuthUser = await _oAuthUserRepository.GetByRefreshTokenAsync(refreshToken);
            if (oAuthUser != null)
            {
                var (accessToken, newRefreshToken) = GenerateTokens(oAuthUser);
                oAuthUser.RefreshToken = newRefreshToken;
                await _oAuthUserRepository.UpdateAsync(oAuthUser);
                return (accessToken, newRefreshToken);
            }

            throw new Exception("Invalid or expired refresh token.");
        }

        public async Task LogoutAsync(string refreshToken)
        {
            if (!string.IsNullOrEmpty(refreshToken))
            {
                var user = await _userRepository.GetByRefreshTokenAsync(refreshToken);
                if (user != null)
                {
                    user.RefreshToken = null;
                    user.RefreshTokenExpiry = null;
                    await _userRepository.UpdateAsync(user);
                }
                else
                {
                    var oAuthUser = await _oAuthUserRepository.GetByRefreshTokenAsync(refreshToken);
                    if (oAuthUser != null)
                    {
                        oAuthUser.RefreshToken = null;
                        await _oAuthUserRepository.UpdateAsync(oAuthUser);
                    }
                }
            }
        }

        public async Task<UserDto> GetCurrentUserAsync(ClaimsIdentity identity)
        {
            if (identity == null || !identity.IsAuthenticated)
                throw new Exception("User is not authenticated.");

            var email = identity.FindFirst(ClaimTypes.Email)?.Value;
            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null)
            {
                var oAuthUser = await _oAuthUserRepository.GetByEmailAsync(email);
                if (oAuthUser != null)
                    return _mapper.Map<UserDto>(oAuthUser);
                throw new Exception("User not found.");
            }
            return _mapper.Map<UserDto>(user);
        }

        private (string AccessToken, string RefreshToken) GenerateTokens(User user)
        {
            var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]);
            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Role, user.Role),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim("UserId", user.UserId.ToString())
                }),
                Expires = DateTime.UtcNow.AddHours(1),
                Issuer = _configuration["Jwt:Issuer"],
                Audience = _configuration["Jwt:Audience"],
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var accessToken = tokenHandler.WriteToken(token);
            var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

            return (accessToken, refreshToken);
        }

        private (string AccessToken, string RefreshToken) GenerateTokens(OAuthUser oAuthUser)
        {
            var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]);
            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Name, oAuthUser.Username),
                    new Claim(ClaimTypes.Role, oAuthUser.Role),
                    new Claim(ClaimTypes.Email, oAuthUser.Email),
                    new Claim("UserId", oAuthUser.OAuthUserId.ToString())
                }),
                Expires = DateTime.UtcNow.AddHours(1),
                Issuer = _configuration["Jwt:Issuer"],
                Audience = _configuration["Jwt:Audience"],
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var accessToken = tokenHandler.WriteToken(token);
            var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

            return (accessToken, refreshToken);
        }

        private string GenerateJwtTokenForOAuthUser(OAuthUser oAuthUser)
        {
            var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]);
            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Name, oAuthUser.Username),
                    new Claim(ClaimTypes.Role, oAuthUser.Role),
                    new Claim(ClaimTypes.Email, oAuthUser.Email),
                    new Claim("UserId", oAuthUser.OAuthUserId.ToString())
                }),
                Expires = DateTime.UtcNow.AddHours(1),
                Issuer = _configuration["Jwt:Issuer"],
                Audience = _configuration["Jwt:Audience"],
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}
