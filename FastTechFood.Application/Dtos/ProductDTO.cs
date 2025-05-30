using FastTechFood.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace FastTechFood.Application.Dtos
{
    public record ProductDTO
    {
        [Required(ErrorMessage = "Id é obrigatório")]
        public Guid Id { get; set; }

        [Required(ErrorMessage = "Nome é obrigatório")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Descrição é obrigatório")]
        public string Description { get; set; }

        public decimal Price { get; set; }
        public bool IsActive { get; set; }
        public ProductType ProductType { get; set; }

        public ProductDTO() { }

        public ProductDTO(Guid id, string name, string description, decimal price, bool isActive, ProductType productType)
        {
            this.Id = id;
            this.Name = name;
            this.Description = description;
            this.Price = price;
            this.IsActive = isActive;
            this.ProductType = productType;
        }
    }
}
