namespace FastTechFood.Application.Interfaces
{
    public interface IValidationService
    {
        bool ValidateEmail(string email);
        bool ValidateCPF(string cpf);
    }
}
