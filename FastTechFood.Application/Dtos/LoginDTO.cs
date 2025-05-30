using System.ComponentModel.DataAnnotations;

namespace FastTechFood.Application.Dtos
{
    public record LoginDTO
    {
        [Required(ErrorMessage = "E-mail é obrigatório")]
        [EmailAddress(ErrorMessage = "E-mail inválido")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Senha é obrigatória")]
        public string Password { get; set; }

        public LoginDTO() { }

        public LoginDTO(string email, string password)
        {
            this.Email = email;
            this.Password = password;
        }
    }
}