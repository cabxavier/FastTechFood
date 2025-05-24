using FastTechFood.Domain.Enums;
using FastTechFood.Domain.Exceptions;
using FastTechFood.Domain.ValueObjects;

namespace FastTechFood.Domain.Entities
{
    public class Order : EntityBase
    {
        public Guid CustomerId { get; private set; }
        public DateTime CreationDate { get; private set; }
        public OrderStatus Status { get; private set; }
        public DeliveryType DeliveryType { get; private set; }
        public string? CancellationReason { get; private set; }
        public List<OrderItem> Items { get; private set; } = new();
        public decimal Total => this.Items.Sum(x => x.Total);

        public Order(Guid customerId, DeliveryType deliveryType)
        {
            this.CustomerId = customerId;
            this.DeliveryType = deliveryType;
            this.CreationDate = DateTime.UtcNow;
            this.Status = OrderStatus.Pending;

            this.Validate();
        }

        public void AddItem(Product product, int quantity)
        {
            var orderItem = this.Items.FirstOrDefault(x => x.ProductId == product.Id);

            if (orderItem is not null)
            {
                orderItem.IncreaseQuantity(quantity);
            }
            else
            {
                this.Items.Add(new OrderItem(product.Id, product.Name, product.Price, quantity));
            }
        }

        public void Accept()
        {
            if (this.Status != OrderStatus.Pending) throw new DomainException("Só é possível pedidos pendentes");

            this.Status = OrderStatus.Accepted;
        }

        public void Reject()
        {
            if (this.Status != OrderStatus.Pending) throw new DomainException("Só é possível rejeitar pedidos pendentes");

            this.Status = OrderStatus.Rejected;
        }

        public void Cancel(string reason)
        {
            if (this.Status != OrderStatus.Pending) throw new DomainException("Só é possível cancelar pedidos pendentes");

            this.Status = OrderStatus.Canceled;

            this.CancellationReason = reason;
        }

        private void Validate()
        {
            if (this.CustomerId == Guid.Empty) throw new DomainException("Cliente é obrigatório");
        }

    }
}
