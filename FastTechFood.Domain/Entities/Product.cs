using FastTechFood.Domain.Enums;
using FastTechFood.Domain.Exceptions;
using FastTechFood.Domain.ValueObjects;

namespace FastTechFood.Domain.Entities
{
    public class Product : EntityBase
    {
        public string Name { get; private set; }
        public string Description { get; private set; }
        public decimal Price { get; private set; }
        public bool IsActive { get; private set; }
        public ProductType ProductType { get; private set; }

        public Product(string name, string description, decimal price, ProductType productType)
        {
            this.Name = name;
            this.Description = description;
            this.Price = price;
            this.ProductType = productType;
            this.IsActive = true;

            this.Validate();
        }

        public Product(string name, string description, decimal price, ProductType productType, bool isActive)
        {
            this.Name = name;
            this.Description = description;
            this.Price = price;
            this.ProductType = productType;
            this.IsActive = isActive;

            this.Validate();
        }

        private void Validate()
        {
            if (string.IsNullOrWhiteSpace(this.Name)) throw new DomainException("Nome do produto é obrigatório");

            if (this.Price <= 0) throw new DomainException("Preço deve ser maior que zero");
        }
    }
}