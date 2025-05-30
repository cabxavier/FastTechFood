using System.ComponentModel.DataAnnotations;

namespace FastTechFood.Application.Dtos
{
    public record CreateOrderItemDTO
    {
        [Required(ErrorMessage = "Produto é obrigatório")]
        public Guid ProductId { get; set; }

        public int Quantity { get; set; }

        public CreateOrderItemDTO() { }

        public CreateOrderItemDTO(Guid productId, int quantity)
        {
            this.ProductId = productId;
            this.Quantity = quantity;
        }
    }
}
