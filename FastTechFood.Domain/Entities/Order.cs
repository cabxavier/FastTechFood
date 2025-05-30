using FastTechFood.Domain.Enums;
using FastTechFood.Domain.Exceptions;
using FastTechFood.Domain.ValueObjects;

namespace FastTechFood.Domain.Entities
{
    public class Order : EntityBase
    {
        public Guid CustomerId { get; private set; }
        public DateTime CreationDate { get; set; }
        public OrderStatus OrderStatus { get; private set; }
        public DeliveryType DeliveryType { get; private set; }
        public string? CancellationReason { get; private set; }
        public List<OrderItem> Items { get; private set; } = new();
        public decimal Total => this.Items.Sum(x => x.Total);

        public Order(Guid customerId, DeliveryType deliveryType, List<OrderItem> items = null)
        {
            this.CustomerId = customerId;
            this.DeliveryType = deliveryType;
            this.Items = items??new List<OrderItem> { };
            this.CreationDate = DateTime.UtcNow;
            this.OrderStatus = OrderStatus.Pending;

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
            if (this.OrderStatus != OrderStatus.Pending) throw new DomainException("Só é possível pedidos pendentes");

            this.OrderStatus = OrderStatus.Accepted;
        }

        public void Reject()
        {
            if (this.OrderStatus != OrderStatus.Pending) throw new DomainException("Só é possível rejeitar pedidos pendentes");

            this.OrderStatus = OrderStatus.Rejected;
        }

        public void Cancel(string reason)
        {
            if (this.OrderStatus != OrderStatus.Pending) throw new DomainException("Só é possível cancelar pedidos pendentes");

            this.OrderStatus = OrderStatus.Canceled;

            this.CancellationReason = reason;
        }

        private void Validate()
        {
            if (this.CustomerId == Guid.Empty) throw new DomainException("Cliente é obrigatório");

            if ((this.OrderStatus == OrderStatus.Canceled) && (string.IsNullOrWhiteSpace(this.CancellationReason)))
                throw new DomainException("OrderStatus é de cancelamento é necessário informar o motivo do cancelamento");
        }
    }
}
