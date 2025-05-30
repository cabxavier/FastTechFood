using FastTechFood.Application.Dtos;
using FastTechFood.Application.Interfaces;
using FastTechFood.Domain.Entities;
using FastTechFood.Domain.Enums;
using FastTechFood.Domain.Exceptions;
using FastTechFood.Domain.Interfaces;
using FastTechFood.Infrastructure.Interfaces;
using Microsoft.Extensions.Logging;

namespace FastTechFood.Application.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository userRepository;
        private readonly IJwtTokenService jwtTokenService;
        private readonly ILogger<UserService> logger;
        private readonly IValidationService validationService;

        public UserService(IUserRepository userRepository, IJwtTokenService jwtTokenService, ILogger<UserService> logger, IValidationService validationService)
        {
            this.userRepository = userRepository;
            this.jwtTokenService = jwtTokenService;
            this.logger = logger;
            this.validationService = validationService;
        }

        public async Task RegisterCustomerAsync(RegisterCustomerDTO registerCustomerDTO)
        {
            this.logger.LogInformation("Iniciando registro de novo cliente: {Email}", registerCustomerDTO.Email);

            if (!this.validationService.ValidateEmail(registerCustomerDTO.Email))
            {
                this.logger.LogWarning("E-mail inválido: {Email}", registerCustomerDTO.Email);

                throw new DomainException("E-mail inválido");
            }

            if (!this.validationService.ValidateCPF(registerCustomerDTO.CPF))
            {
                this.logger.LogWarning("CPF inválido: {CPF}", registerCustomerDTO.CPF);

                throw new DomainException("CPF inválido");
            }

            if (await this.EmailExistsAsync(registerCustomerDTO.Email))
            {
                this.logger.LogWarning("Tentativa de registro com e-mail já existente: {Email}", registerCustomerDTO.Email);

                throw new DomainException("E-mail já cadastrado");
            }

            if (await this.CpfExistsAsync(registerCustomerDTO.CPF))
            {
                this.logger.LogWarning("Tentativa de registro com CPF já existente: {CPF}", registerCustomerDTO.CPF);

                throw new DomainException("CPF já cadastrado");
            }

            try
            {
                var user = new User(
                    registerCustomerDTO.Name,
                    registerCustomerDTO.Email,
                    BCrypt.Net.BCrypt.HashPassword(registerCustomerDTO.Password),
                    UserType.Customer,
                    registerCustomerDTO.CPF);

                await this.userRepository.AddAsync(user);

                this.logger.LogInformation("Cliente registrado com sucesso: {UserId}", user.Id);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Erro ao registrar o cliente: {Email}", registerCustomerDTO.Email);
                throw new DomainException(ex.Message);
            }
        }

        public async Task RegisterEmployeeAsync(RegisterEmployeeDTO registerEmployeeDTO)
        {
            this.logger.LogInformation("Iniciando registro de novo funcionário: {Email}", registerEmployeeDTO.Email);

            if (!this.validationService.ValidateEmail(registerEmployeeDTO.Email))
            {
                this.logger.LogWarning("E-mail inválido: {Email}", registerEmployeeDTO.Email);

                throw new DomainException("E-mail inválido");
            }

            if (!this.validationService.ValidateCPF(registerEmployeeDTO.CPF))
            {
                this.logger.LogWarning("CPF inválido: {CPF}", registerEmployeeDTO.CPF);

                throw new DomainException("CPF inválido");
            }

            if (await EmailExistsAsync(registerEmployeeDTO.Email))
            {
                this.logger.LogWarning("Tentativa de registro de funcionário com e-mail já existente: {Email}", registerEmployeeDTO.Email);

                throw new DomainException("E-mail já cadastrado");
            }

            try
            {
                var userType = registerEmployeeDTO.Role switch
                {
                    "Gerente" => UserType.Manager,
                    "Cozinha" => UserType.KitchenStaff,
                    _ => UserType.Employee
                };

                var user = new User(
                    registerEmployeeDTO.Name,
                    registerEmployeeDTO.Email,
                    BCrypt.Net.BCrypt.HashPassword(registerEmployeeDTO.Password),
                    userType,
                    registerEmployeeDTO.CPF);

                await this.userRepository.AddAsync(user);

                this.logger.LogInformation("Funcionário registrado com sucesso: {UserId}", user.Id);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Erro ao registrar o funcionário: {Email}", registerEmployeeDTO.Email);
                throw new DomainException(ex.Message);
            }
        }

        public async Task<string> LoginAsync(LoginDTO loginDTO)
        {
            this.logger.LogInformation("Tentativa de login: {Email}", loginDTO.Email);

            var user = await this.userRepository.GetByEmailAsync(loginDTO.Email);

            if (user == null || !BCrypt.Net.BCrypt.Verify(loginDTO.Password, user.Password))
            {
                this.logger.LogWarning("Falha na autenticação para o e-mail: {Email}", loginDTO.Email);

                throw new DomainException("Credenciais inválidas");
            }

            try
            {
                var token = this.jwtTokenService.GenerateToken(user);

                this.logger.LogInformation("Login bem-sucedido para o usuário: {UserId}", user.Id);

                return token;
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Erro ao gerar token JWT para o usuário: {UserId}", user?.Id);

                throw new DomainException("Erro ao processar o login");
            }
        }

        public async Task<UserDTO?> GetUserByIdAsync(Guid id)
        {
            this.logger.LogInformation("Buscando usuário por Id: {UserId}", id);

            var user = await this.userRepository.GetByIdAsync(id);

            if (user == null)
            {
                return null;
            }

            var userDTO = new UserDTO
            (
                user.Id,
                user.Name,
                user.Email,
                user.UserType,
                user.CPF
            );

            return userDTO;
        }

        public async Task UpdateUserAsync(User user)
        {
            this.logger.LogInformation("Atualizando usuário: {UserId}", user.Id);

            if ((await this.userRepository.GetByIdAsync(user.Id)) is null)
            {
                this.logger.LogWarning("Usuário não encontrado para atualização: {UserId}", user.Id);

                throw new DomainException("Usuário não encontrado");
            }
            try
            {
                await this.userRepository.UpdateAsync(user);

                this.logger.LogInformation("Usuário atualizado com sucesso: {UserId}", user.Id);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Erro ao atualizar o usuário: {UserId}", user.Id);

                throw new DomainException(ex.Message);
            }
        }

        public async Task<bool> EmailExistsAsync(string email) => (await this.userRepository.GetByEmailAsync(email)) != null;

        public async Task<bool> CpfExistsAsync(string cpf) => (await this.userRepository.GetByCpfAsync(cpf)) != null;

        public async Task ChangePasswordAsync(Guid userId, string currentPassword, string newPassword)
        {
            this.logger.LogInformation("Alteração de senha solicitada para o usuário: {UserId}", userId);

            var user = await this.userRepository.GetByIdAsync(userId);

            if (user == null)
            {
                this.logger.LogWarning("Usuário não encontrado para alteração de senha: {UserId}", userId);

                throw new DomainException("Usuário não encontrado");
            }

            if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.Password))
            {
                this.logger.LogWarning("Senha atual incorreta para o usuário: {UserId}", userId);

                throw new DomainException("Senha atual incorreta");
            }

            user.Password = BCrypt.Net.BCrypt.HashPassword(newPassword);

            await this.userRepository.UpdateAsync(user);

            this.logger.LogInformation("Senha alterada com sucesso para o usuário: {UserId}", userId);
        }
    }
}