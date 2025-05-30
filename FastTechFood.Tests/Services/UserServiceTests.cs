using Moq;
using Microsoft.Extensions.Logging;
using FastTechFood.Application.Dtos;
using FastTechFood.Application.Interfaces;
using FastTechFood.Application.Services;
using FastTechFood.Domain.Entities;
using FastTechFood.Domain.Enums;
using FastTechFood.Domain.Exceptions;
using FastTechFood.Domain.Interfaces;
using FastTechFood.Infrastructure.Interfaces;

namespace FastTechFood.Application.Tests.Services
{
    public class UserServiceTests
    {
        private readonly Mock<IUserRepository> userRepositoryMock;
        private readonly Mock<IJwtTokenService> jwtTokenServiceMock;
        private readonly Mock<ILogger<UserService>> loggerMock;
        private readonly Mock<IValidationService> validationServiceMock;
        private readonly UserService userService;

        public UserServiceTests()
        {
            this.userRepositoryMock = new Mock<IUserRepository>();
            this.jwtTokenServiceMock = new Mock<IJwtTokenService>();
            this.loggerMock = new Mock<ILogger<UserService>>();
            this.validationServiceMock = new Mock<IValidationService>();

            this.userService = new UserService(this.userRepositoryMock.Object, this.jwtTokenServiceMock.Object, this.loggerMock.Object, this.validationServiceMock.Object);
        }

        [Fact]
        public async Task RegisterCustomerAsync_WithValidData_ShouldRegisterSuccessfully()
        {
            var registerDto = new RegisterCustomerDTO
            (
                "John Doe",
                "john.doe@example.com",
                 "Password123!",
                 "12345678909"
            );

            this.validationServiceMock.Setup(x => x.ValidateEmail(registerDto.Email)).Returns(true);
            this.validationServiceMock.Setup(x => x.ValidateCPF(registerDto.CPF)).Returns(true);
            this.userRepositoryMock.Setup(x => x.GetByEmailAsync(registerDto.Email)).ReturnsAsync((User)null);
            this.userRepositoryMock.Setup(x => x.GetByCpfAsync(registerDto.CPF)).ReturnsAsync((User)null);
            this.userRepositoryMock.Setup(x => x.AddAsync(It.IsAny<User>())).Returns(Task.CompletedTask);

            await this.userService.RegisterCustomerAsync(registerDto);

            this.userRepositoryMock.Verify(x => x.AddAsync(It.Is<User>(u =>
                u.Name == registerDto.Name &&
                u.Email == registerDto.Email &&
                u.UserType == UserType.Customer)),
                Times.Once);
        }

        [Fact]
        public async Task RegisterCustomerAsync_WithInvalidEmail_ShouldThrowDomainException()
        {
            var registerDto = new RegisterCustomerDTO
            (
                "John Doe",
                "invalid-email",
                "Password123!",
                "12345678909"
            );

            this.validationServiceMock.Setup(x => x.ValidateEmail(registerDto.Email)).Returns(false);

            await Assert.ThrowsAsync<DomainException>(() => this.userService.RegisterCustomerAsync(registerDto));
        }

        [Fact]
        public async Task RegisterCustomerAsync_WithExistingEmail_ShouldThrowDomainException()
        {
            var registerDto = new RegisterCustomerDTO
            (
                "John Doe",
                "existing@example.com",
                "Password123!",
                "12345678909"
            );

            var existingUser = new User("Existing User", registerDto.Email, "hashed", UserType.Customer, registerDto.CPF);

            this.validationServiceMock.Setup(x => x.ValidateEmail(registerDto.Email)).Returns(true);
            this.validationServiceMock.Setup(x => x.ValidateCPF(registerDto.CPF)).Returns(true);
            this.userRepositoryMock.Setup(x => x.GetByEmailAsync(registerDto.Email)).ReturnsAsync(existingUser);

            await Assert.ThrowsAsync<DomainException>(() => this.userService.RegisterCustomerAsync(registerDto));
        }

        [Theory]
        [InlineData("Gerente", UserType.Manager)]
        [InlineData("Cozinha", UserType.KitchenStaff)]
        [InlineData("Funcionario", UserType.Employee)]
        public async Task RegisterEmployeeAsync_WithDifferentRoles_ShouldSetCorrectUserType(string role, UserType expectedUserType)
        {
            var registerDto = new RegisterEmployeeDTO
            (
                "Jane Doe",
                "jane.doe@example.com",
                "Password123!",
                role,
                "98765432109"
            );

            this.validationServiceMock.Setup(x => x.ValidateEmail(registerDto.Email)).Returns(true);
            this.validationServiceMock.Setup(x => x.ValidateCPF(registerDto.CPF)).Returns(true);
            this.userRepositoryMock.Setup(x => x.GetByEmailAsync(registerDto.Email)).ReturnsAsync((User)null);

            User capturedUser = null;
            this.userRepositoryMock.Setup(x => x.AddAsync(It.IsAny<User>()))
                .Callback<User>(user => capturedUser = user)
                .Returns(Task.CompletedTask);

            await this.userService.RegisterEmployeeAsync(registerDto);

            this.userRepositoryMock.Verify(x => x.AddAsync(It.IsAny<User>()), Times.Once);
            Assert.NotNull(capturedUser);
            Assert.Equal(expectedUserType, capturedUser.UserType);
        }

        [Fact]
        public async Task LoginAsync_WithValidCredentials_ShouldReturnToken()
        {
            var loginDto = new LoginDTO
            (
                "valid@example.com",
                "correctPassword"
            );

            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(loginDto.Password);
            var user = new User("Test User", loginDto.Email, hashedPassword, UserType.Customer, "12345678909");

            this.userRepositoryMock.Setup(x => x.GetByEmailAsync(loginDto.Email)).ReturnsAsync(user);
            this.jwtTokenServiceMock.Setup(x => x.GenerateToken(user)).Returns("generated-token");

            Assert.Equal("generated-token", await this.userService.LoginAsync(loginDto));
        }

        [Fact]
        public async Task LoginAsync_WithInvalidPassword_ShouldThrowDomainException()
        {
            var loginDto = new LoginDTO
            (
                "valid@example.com",
                "wrongPassword"
            );

            var hashedPassword = BCrypt.Net.BCrypt.HashPassword("correctPassword");
            var user = new User("Test User", loginDto.Email, hashedPassword, UserType.Customer, "12345678909");

            this.userRepositoryMock.Setup(x => x.GetByEmailAsync(loginDto.Email)).ReturnsAsync(user);

            await Assert.ThrowsAsync<DomainException>(() => this.userService.LoginAsync(loginDto));
        }

        [Fact]
        public async Task LoginAsync_WithNonExistentEmail_ShouldThrowDomainException()
        {
            var loginDto = new LoginDTO
            (
                "nonexistent@example.com",
                "anyPassword"
            );

            this.userRepositoryMock.Setup(x => x.GetByEmailAsync(loginDto.Email)).ReturnsAsync((User)null);

            await Assert.ThrowsAsync<DomainException>(() => this.userService.LoginAsync(loginDto));
        }

        [Fact]
        public async Task GetUserByIdAsync_WithExistingUser_ShouldReturnUserDTO()
        {
            var userId = Guid.NewGuid();
            var user = new User("Test User", "test@example.com", "hashed", UserType.Customer, "12345678909") { Id = userId };

            this.userRepositoryMock.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);

            var result = await this.userService.GetUserByIdAsync(userId);

            Assert.NotNull(result);
            Assert.Equal(userId, result.Id);
            Assert.Equal(user.Name, result.Name);
            Assert.Equal(user.Email, result.Email);
        }

        [Fact]
        public async Task GetUserByIdAsync_WithNonExistentUser_ShouldReturnNull()
        {
            var userId = Guid.NewGuid();
            this.userRepositoryMock.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync((User)null);

            Assert.Null(await this.userService.GetUserByIdAsync(userId));
        }

        [Fact]
        public async Task UpdateUserAsync_WithExistingUser_ShouldUpdateSuccessfully()
        {
            var userId = Guid.NewGuid();
            var existingUser = new User("Old Name", "old@example.com", "hashed", UserType.Customer, "12345678909") { Id = userId };
            var updatedUser = new User("New Name", "new@example.com", "newHashed", UserType.Customer, "12345678909") { Id = userId };

            this.userRepositoryMock.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(existingUser);
            this.userRepositoryMock.Setup(x => x.UpdateAsync(updatedUser)).Returns(Task.CompletedTask);

            await this.userService.UpdateUserAsync(updatedUser);

            this.userRepositoryMock.Verify(x => x.UpdateAsync(updatedUser), Times.Once);
        }

        [Fact]
        public async Task UpdateUserAsync_WithNonExistentUser_ShouldThrowDomainException()
        {
            var userId = Guid.NewGuid();
            var user = new User("Test", "test@example.com", "hashed", UserType.Customer, "12345678909") { Id = userId };

            this.userRepositoryMock.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync((User)null);

            await Assert.ThrowsAsync<DomainException>(() => this.userService.UpdateUserAsync(user));
        }

        [Fact]
        public async Task EmailExistsAsync_WithExistingEmail_ShouldReturnTrue()
        {
            var email = "existing@example.com";
            var user = new User("Test", email, "hashed", UserType.Customer, "12345678909");

            this.userRepositoryMock.Setup(x => x.GetByEmailAsync(email)).ReturnsAsync(user);

            var result = await this.userService.EmailExistsAsync(email);

            Assert.True(result);
        }

        [Fact]
        public async Task EmailExistsAsync_WithNonExistingEmail_ShouldReturnFalse()
        {
            var email = "nonexisting@example.com";
            this.userRepositoryMock.Setup(x => x.GetByEmailAsync(email)).ReturnsAsync((User)null);

            Assert.False(await this.userService.EmailExistsAsync(email));
        }

        [Fact]
        public async Task CpfExistsAsync_WithExistingCpf_ShouldReturnTrue()
        {
            var cpf = "12345678909";

            this.userRepositoryMock.Setup(x => x.GetByCpfAsync(cpf)).ReturnsAsync(new User("Test", "test@example.com", "hashed", UserType.Customer, cpf));

            Assert.True(await this.userService.CpfExistsAsync(cpf));
        }

        [Fact]
        public async Task CpfExistsAsync_WithNonExistingCpf_ShouldReturnFalse()
        {
            var cpf = "98765432109";
            this.userRepositoryMock.Setup(x => x.GetByCpfAsync(cpf)).ReturnsAsync((User)null);

            Assert.False(await this.userService.CpfExistsAsync(cpf));
        }

        [Fact]
        public async Task ChangePasswordAsync_WithValidCurrentPassword_ShouldUpdatePassword()
        {
            var userId = Guid.NewGuid();
            var currentPassword = "currentPassword";
            var newPassword = "newPassword";
            var hashedCurrentPassword = BCrypt.Net.BCrypt.HashPassword(currentPassword);

            User updatedUser = null;

            this.userRepositoryMock.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(new User("Test", "test@example.com", hashedCurrentPassword, UserType.Customer, "12345678909") { Id = userId });
            this.userRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<User>()))
                .Callback<User>(u => updatedUser = u)
                .Returns(Task.CompletedTask);

            await this.userService.ChangePasswordAsync(userId, currentPassword, newPassword);

            this.userRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<User>()), Times.Once);

            Assert.NotNull(updatedUser);
            Assert.Equal(userId, updatedUser.Id);
            Assert.True(BCrypt.Net.BCrypt.Verify(newPassword, updatedUser.Password));
        }

        [Fact]
        public async Task ChangePasswordAsync_WithInvalidCurrentPassword_ShouldThrowDomainException()
        {
            var userId = Guid.NewGuid();
            var currentPassword = "wrongPassword";
            var newPassword = "newPassword";
            var hashedCurrentPassword = BCrypt.Net.BCrypt.HashPassword("correctPassword");

            this.userRepositoryMock.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(new User("Test", "test@example.com", hashedCurrentPassword, UserType.Customer, "12345678909") { Id = userId });

            await Assert.ThrowsAsync<DomainException>(() =>
                this.userService.ChangePasswordAsync(userId, currentPassword, newPassword));
        }

        [Fact]
        public async Task ChangePasswordAsync_WithNonExistentUser_ShouldThrowDomainException()
        {
            var userId = Guid.NewGuid();
            this.userRepositoryMock.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync((User)null);

            await Assert.ThrowsAsync<DomainException>(() =>
                this.userService.ChangePasswordAsync(userId, "current", "new"));
        }
    }
}