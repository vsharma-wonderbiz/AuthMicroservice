using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthMicroservice.Application.Interface
{
    public interface IEamilService
    {
        Task SendEmailAsync(string toEmail, string subject, string message);
    }
}
