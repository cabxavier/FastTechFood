using System.ComponentModel.DataAnnotations;

namespace FastTechFood.Application.Dtos
{
    public record RegisterCustomerDTO
    {
        [Required(ErrorMessage = "Nome é obrigatório")]
        public string Name { get; set; }

        [Required(ErrorMessage = "E-mail é obrigatório")]
        [EmailAddress(ErrorMessage = "E-mail inválido")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Senha é obrigatória")]
        public string Password { get; set; }

        [Required(ErrorMessage = "CPF é obrigatóriO")]
        public string CPF { get; set; }

        public RegisterCustomerDTO() { }

        public RegisterCustomerDTO(string name, string email, string password, string cpf)
        {
            this.Name = name;
            this.Email = email;
            this.Password = password;
            this.CPF = cpf;
        }
    }
}
