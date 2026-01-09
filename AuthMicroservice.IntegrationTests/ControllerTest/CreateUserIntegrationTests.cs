using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using AuthMicroservice.Application.Dtos;
using AuthMicroservice.IntegrationTests.CustomWebApplication;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AuthMicroservice.IntegrationTests.ControllerTest
{
    //IClassFixture is basically sued to maek the instance of customeWebAllicationFactory once and sahre them among all the test in the function 
    public class CreateUserIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client;

        public CreateUserIntegrationTests(CustomWebApplicationFactory factory)
        {
            _client = factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });
        }

        [Fact]
        public async Task RegitserUSer_ValidInput_ReturnsCreated_AndSaved()
        {
            var dto = new CreateUserDto
            {
                Username = "vinay_21",
                Email ="Vinay@Gmail.com",
                Password="Vinay@123",
            };

            var response = await _client.PostAsJsonAsync("/api/User/Register", dto);

            Assert.Equal(HttpStatusCode.Created,response.StatusCode);

            var createdUser = await response.Content
           .ReadFromJsonAsync<UserDto>();

            Assert.NotNull(createdUser);
            Assert.Equal(dto.Username, createdUser.Username);
            Assert.Equal(dto.Email, createdUser.Email);
            Assert.Equal("User", createdUser.Role);
        }
    }
}
