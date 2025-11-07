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
    public class UserOtpRepository : IUserOtpRepository
    {
        private readonly UserDbContext _dbContext;

        public UserOtpRepository(UserDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<UserOtp> GetLatestOtp(string email)
        {
           return await _dbContext.UserOtp
                .Where(x => x.Email == email && x.IsUsed == false)
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync();
        }

        public async Task  AddOtp(UserOtp otp)
        {
            await _dbContext.UserOtp.AddAsync(otp);
            await _dbContext.SaveChangesAsync();
        }

        public async Task MarkUse(Guid id)
        {
            var entity =await _dbContext.UserOtp.FindAsync(id);
              if(entity!=null)
            {
                entity.IsUsed = true;
                await _dbContext.SaveChangesAsync();
            }
        }

        public async Task DeleteOldOtp(string email)
        {
            var oldotp = _dbContext.UserOtp.Where(x => x.Email == email);
             _dbContext.RemoveRange(oldotp);
            await _dbContext.SaveChangesAsync();
        }
    }
}
