using FastTechFood.Application.Dtos;
using FastTechFood.Application.Interfaces;

namespace FastTechFood.Messaging.Consumers
{
    public class PedidoConsumerHandler : IConsumer<CreateOrderDTO>
    {
        private readonly IOrderService orderService;

        public PedidoConsumerHandler(IOrderService orderService)
        {
            this.orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
        }

        public async Task HandleAsync(CreateOrderDTO createOrderDTO)
        {
            var orderDTO = await this.orderService.CreateOrderAsync(createOrderDTO);
        }
    }
}