using FastTechFood.Domain.Enums;
using FastTechFood.Domain.Exceptions;
using FastTechFood.Domain.ValueObjects;

namespace FastTechFood.Domain.Entities
{
    public class User : EntityBase
    {
        public string Name { get; private set; }
        public string Email { get; private set; }
        public string PasswordHash { get; private set; }
        public UserType UserType { get; private set; }
        public string? CPF { get; private set; }


        public User(string name, string email, string passwordHash, UserType userType)
        {
            this.Name = name;
            this.Email = email;
            this.PasswordHash = passwordHash;
            this.UserType = userType;

            this.Validate();
        }

        public User(string name, string email, string passwordHash, UserType userType, string cpf)
        {
            this.Name = name;
            this.Email = email;
            this.PasswordHash = passwordHash;
            this.UserType = userType;
            this.CPF = cpf;

            this.Validate();
        }

        private void Validate()
        {
            if (string.IsNullOrWhiteSpace(this.Name)) throw new DomainException("Nome é obrigatório");

            if (string.IsNullOrWhiteSpace(this.Email)) throw new DomainException("E-mail é obrigatório");

            if (string.IsNullOrWhiteSpace(this.PasswordHash)) throw new DomainException("Senha é obrigatória");
        }
    }
}
