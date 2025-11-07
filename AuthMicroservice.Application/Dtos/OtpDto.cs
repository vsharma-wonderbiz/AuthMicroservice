using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthMicroservice.Application.Dtos
{
    public class OtpDto
    {
        public string Email { get; set; }
        public int Otp { get; set; }
    }
}
