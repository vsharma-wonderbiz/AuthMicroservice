using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthMicroservice.Domain.Entities
{
    public class OAuthUser
    {
        public int OAuthUserId { get; set; }
        public string Email { get; set; }
        public string Username { get; set; }
        public string Role { get; set; } // e.g., "User", "Admin"
        public string GoogleId { get; set; } // Unique Google user ID
        public string AccessToken { get; set; } // Optional: Store Google access token
        public string? RefreshToken { get; set; }
    }
}
