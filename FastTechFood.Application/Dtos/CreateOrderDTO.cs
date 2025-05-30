using FastTechFood.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace FastTechFood.Application.Dtos
{
    public record CreateOrderDTO
    {
        [Required(ErrorMessage = "Cliente é obrigatório")]
        public Guid CustomerId { get; set; }

        [Required(ErrorMessage = "DeliveryType é obrigatório")]
        public DeliveryType DeliveryType { get; set; }

        [Required(ErrorMessage = "Items é obrigatório")]
        public List<CreateOrderItemDTO> Items { get; set; }

        public CreateOrderDTO() { }

        public CreateOrderDTO(Guid customerId, DeliveryType deliveryType, List<CreateOrderItemDTO> items)
        {
            this.CustomerId = customerId;
            this.DeliveryType = deliveryType;
            this.Items = items;
        }
    }
}
