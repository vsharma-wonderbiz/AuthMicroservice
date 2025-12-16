using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthMicroservice.Application.Dtos
{
    public class UpdateUserDto
    {
        public string Username { get; set; }
        public string Email { get; set; }
        public string? Password { get; set; }
        public string? Role { get; set; }
    }

    public class UpadateUserRole
    {
        public string Username { get; set; }
        public string Email { get; set; }
        public string? Role { get; set; }
    }
}
