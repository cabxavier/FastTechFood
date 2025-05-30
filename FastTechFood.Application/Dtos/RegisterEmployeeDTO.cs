using FastTechFood.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace FastTechFood.Application.Dtos
{
    public record RegisterEmployeeDTO
    {
        [Required(ErrorMessage = "Nome é obrigatório")]
        public string Name { get; set; }

        [Required(ErrorMessage = "E-mail é obrigatório")]
        [EmailAddress(ErrorMessage = "E-mail inválido")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Senha é obrigatória")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Role é obrigatório")]
        public string Role { get;set; }

        [Required(ErrorMessage = "CPF é obrigatóriO")]
        public string CPF { get; set; }

        public RegisterEmployeeDTO() { }

        public RegisterEmployeeDTO(string name,  string email, string password, string role, string cpf)
        {
            this.Name = name;
            this.Email = email;
            this.Password = password;
            this.Role = role;
            this.CPF = cpf;
        }
    }
}
