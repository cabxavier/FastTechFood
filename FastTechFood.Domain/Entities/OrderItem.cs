using FastTechFood.Domain.Exceptions;
using FastTechFood.Domain.ValueObjects;

namespace FastTechFood.Domain.Entities
{
    public class OrderItem : EntityBase
    {
        public Guid ProductId { get; private set; }
        public string ProductName { get; set; }
        public decimal UnitPrice { get; private set; }
        public int Quantity { get; private set; }
        public decimal Total => this.UnitPrice * this.Quantity;

        public OrderItem(Guid productId, string productName, decimal unitPrice, int quantity)
        {
            this.ProductId = productId;
            this.ProductName = productName;
            this.UnitPrice = unitPrice;
            this.Quantity = quantity;

            this.Validate();
        }

        public void IncreaseQuantity(int quantity)
        {
            this.Quantity += quantity;

            this.Validate();
        }

        private void Validate()
        {
            if (this.ProductId == Guid.Empty) throw new DomainException("Produto é obrigatório");

            if (string.IsNullOrWhiteSpace(this.ProductName)) throw new DomainException("Descrição do produto é obrigatório");

            if (this.Quantity <= 0) throw new DomainException("Quantidade deve ser maior que zero");

            if (this.UnitPrice <= 0) throw new DomainException("Preço unitário deve ser maior que zero");
        }
    }
}
