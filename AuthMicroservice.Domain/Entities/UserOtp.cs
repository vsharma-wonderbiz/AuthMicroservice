using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthMicroservice.Domain.Entities
{
    public class UserOtp
    {
        [Key]
        public Guid UserOtpID { get; set; }

        [Required]
        public string Email { get; set; }

        [Required]
        public int Otp { get; set; }

        [Required]
        public DateTime ExpiryAt { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }

        public bool IsUsed { get; set; } = false;

    }
}
