using Moq;
using FastTechFood.Application.Dtos;
using FastTechFood.Application.Interfaces;
using FastTechFood.API.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace FastTechFood.API.Tests.Controllers
{
    public class AuthControllerTests
    {
        private readonly Mock<IUserService> userServiceMock;
        private readonly AuthController authController;

        public AuthControllerTests()
        {
            this.userServiceMock = new Mock<IUserService>();
            this.authController = new AuthController(userServiceMock.Object);
        }

        [Fact]
        public async Task RegisterCustomer_WithValidData_ShouldReturnOkResult()
        {
            var registerDto = new RegisterCustomerDTO("John Doe", "john.doe@example.com", "Password123!", "12345678909");

            this.userServiceMock.Setup(x => x.RegisterCustomerAsync(registerDto))
                .Returns(Task.CompletedTask);

            Assert.Equal("Cliente registrado com sucesso", Assert.IsType<OkObjectResult>(await this.authController.RegisterCustomer(registerDto)).Value);
        }

        [Fact]
        public async Task RegisterCustomer_WithInvalidData_ShouldReturnBadRequest()
        {
            var registerDto = new RegisterCustomerDTO("John Doe", "invalid-email", "Password123!", "12345678909");

            this.userServiceMock.Setup(x => x.RegisterCustomerAsync(registerDto))
                .ThrowsAsync(new Exception("E-mail inválido"));

            Assert.Equal("E-mail inválido", Assert.IsType<BadRequestObjectResult>(await this.authController.RegisterCustomer(registerDto)).Value);
        }

        [Fact]
        public async Task RegisterEmployee_WithValidData_ShouldReturnOkResult()
        {
            var registerDto = new RegisterEmployeeDTO("Jane Doe", "jane.doe@example.com", "Password123!", "Gerente", "98765432109");

            this.userServiceMock.Setup(x => x.RegisterEmployeeAsync(registerDto))
                .Returns(Task.CompletedTask);

            Assert.Equal("Funcionário registrado com sucesso", Assert.IsType<OkObjectResult>(await this.authController.RegisterEmployee(registerDto)).Value);
        }

        [Fact]
        public async Task RegisterEmployee_WithInvalidData_ShouldReturnBadRequest()
        {
            var registerDto = new RegisterEmployeeDTO("Jane Doe", "invalid-email", "Password123!", "Gerente", "98765432109");

            this.userServiceMock.Setup(x => x.RegisterEmployeeAsync(registerDto))
                .ThrowsAsync(new Exception("E-mail inválido"));

            Assert.Equal("E-mail inválido", Assert.IsType<BadRequestObjectResult>(await this.authController.RegisterEmployee(registerDto)).Value);
        }

        [Fact]
        public async Task Login_WithValidCredentials_ShouldReturnOkWithToken()
        {
            var loginDto = new LoginDTO("valid@example.com", "Password123!");

            this.userServiceMock.Setup(x => x.LoginAsync(loginDto))
                .ReturnsAsync("fake-jwt-token");

            Assert.Equal("fake-jwt-token", Assert.IsType<AuthDTO>(Assert.IsType<OkObjectResult>(await this.authController.Login(loginDto)).Value).Token);
        }

        [Fact]
        public async Task Login_WithInvalidCredentials_ShouldReturnUnauthorized()
        {
            var loginDto = new LoginDTO("invalid@example.com", "WrongPassword");

            this.userServiceMock.Setup(x => x.LoginAsync(loginDto))
                .ThrowsAsync(new Exception("Credenciais inválidas"));

            Assert.Equal("Credenciais inválidas", Assert.IsType<UnauthorizedObjectResult>(await this.authController.Login(loginDto)).Value);
        }

        [Fact]
        public async Task Login_WithException_ShouldReturnUnauthorized()
        {
            var loginDto = new LoginDTO("error@example.com", "Password123!");

            this.userServiceMock.Setup(x => x.LoginAsync(loginDto))
                .ThrowsAsync(new Exception("Erro no servidor"));

            Assert.Equal("Erro no servidor", Assert.IsType<UnauthorizedObjectResult>(await this.authController.Login(loginDto)).Value);
        }

        [Fact]
        public async Task RegisterCustomer_WithNullDto_ShouldReturnBadRequest()
        {
            Assert.IsType<BadRequestObjectResult>(await this.authController.RegisterCustomer(null));
        }

        [Fact]
        public async Task RegisterEmployee_WithNullDto_ShouldReturnBadRequest()
        {
            Assert.IsType<BadRequestObjectResult>(await this.authController.RegisterEmployee(null));
        }

        [Fact]
        public async Task Login_WithNullDto_ShouldReturnBadRequest()
        {
            Assert.IsType<BadRequestObjectResult>(await this.authController.Login(null));
        }

        [Fact]
        public async Task Login_WithInvalidModel_ShouldReturnBadRequest()
        {
            this.authController.ModelState.AddModelError("Email", "Email é obrigatório");

            Assert.IsType<BadRequestObjectResult>(await this.authController.Login(new LoginDTO()));
        }

        [Fact]
        public async Task Login_WithEmptyEmail_ShouldReturnBadRequest()
        {
            Assert.IsType<BadRequestObjectResult>(await this.authController.Login(new LoginDTO(string.Empty, "Password123")));
        }
    }
}