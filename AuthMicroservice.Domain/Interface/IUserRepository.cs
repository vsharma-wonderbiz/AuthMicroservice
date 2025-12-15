using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AuthMicroservice.Domain.Entities;

namespace AuthMicroservice.Domain.Interface
{
    public interface IUserRepository
    {
        Task<List<User>> GetAllAsync();
        Task<User> GetByIdAsync(int id);
        Task<User> GetByEmailAsync(string email);
        Task AddAsync(User user);
        Task<User> GetByRefreshTokenAsync(string refreshToken); // Added
        Task UpdateAsync(User user);
        Task DeleteAsync(int id);
        Task<bool> UsernameExistsAsync(string username, int? excludeId = null);
        Task<bool> EmailExistsAsync(string email, int? excludeId = null);
        Task SaveChangesAsync();
    }
}
