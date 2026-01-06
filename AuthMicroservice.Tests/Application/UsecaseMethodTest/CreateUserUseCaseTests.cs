using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AuthMicroservice.Domain.Interface;
using AutoMapper;
using Moq;

namespace AuthMicroservice.Tests.Application.UsecaseMethodTest
{
    public class CreateUserUseCaseTests
    {
        private readonly Mock<IUserRepository> _mockUserRepository;
        private readonly Mock<IMapper> _mockMapper;
       

        public CreateUserUseCaseTests()
        {
            _mockUserRepository = new Mock<IUserRepository>();
            _mockMapper = new Mock<IMapper>();
           
        }
    }
}
