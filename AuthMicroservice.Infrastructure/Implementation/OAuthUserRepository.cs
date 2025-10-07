using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AuthMicroservice.Domain.Entities;
using AuthMicroservice.Domain.Interface;
using AuthMicroservice.Infrastructure.Persistance.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace AuthMicroservice.Infrastructure.Implementation
{
    public class OAuthUserRepository : IOAuthUserRepository
    {
        private readonly UserDbContext _context;

        public OAuthUserRepository(UserDbContext context)
        {
            _context = context;
        }

        public async Task<OAuthUser> GetByGoogleIdAsync(string googleId)
        {
            //return await _context.OAuthUsers.FirstOrDefaultAsync(u => u.GoogleId == googleId);
            return await _context.OAuthUsers.FirstOrDefaultAsync(u => u.GoogleId == googleId);
        }

        public async Task<OAuthUser> GetByEmailAsync(string email)
        {
            return await _context.OAuthUsers.FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<OAuthUser> GetByRefreshTokenAsync(string refreshToken)
        {
            return await _context.OAuthUsers.FirstOrDefaultAsync(u => u.RefreshToken == refreshToken);
        }

        public async Task AddAsync(OAuthUser oAuthUser)
        {
            _context.OAuthUsers.Add(oAuthUser);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(OAuthUser oAuthUser)
        {
            _context.OAuthUsers.Update(oAuthUser);
            await _context.SaveChangesAsync();
        }
    }
}
