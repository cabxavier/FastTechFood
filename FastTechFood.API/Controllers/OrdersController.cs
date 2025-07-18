﻿using FastTechFood.Application.Dtos;
using FastTechFood.Application.Interfaces;
using FastTechFood.Messaging.Publishers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FastTechFood.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController : Controller
    {
        private readonly IOrderService orderService;
        private readonly IRabbitMQPublisherService rabbitMQPublisherService;

        public OrdersController(IOrderService orderService, IRabbitMQPublisherService rabbitMQPublisherService)
        {
            this.orderService = orderService;
            this.rabbitMQPublisherService = rabbitMQPublisherService;
        }

        [HttpPost]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> Create([FromBody] CreateOrderDTO createOrderDTO)
        {
            try
            {
                if (!User.IsInRole("Customer"))
                    return Forbid();

                if (createOrderDTO == null)
                {
                    return BadRequest("Informe os dados do pedido");
                }

                await this.rabbitMQPublisherService.SendMessageAsync(createOrderDTO, this.rabbitMQPublisherService.GetConfiguration().GetSection("RabbitMQ")["QueueCreateOrder"] ?? string.Empty);

                return Ok(createOrderDTO);
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
        }

        [HttpGet("pending")]
        [Authorize(Roles = "KitchenStaff,Manager")]
        public async Task<IActionResult> GetPendingOrders()
        {
            if (!(User.IsInRole("KitchenStaff") || User.IsInRole("Manager")))
                return Forbid();

            return Ok(await this.orderService.GetPendingOrdersAsync());
        }

        [HttpPut("{id}/accept")]
        [Authorize(Roles = "KitchenStaff,Manager")]
        public async Task<IActionResult> AcceptOrder(Guid id)
        {
            try
            {
                await this.orderService.AcceptOrderAsync(id);

                return Ok("Pedido aceito com sucesso");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("{id}/reject")]
        [Authorize(Roles = "KitchenStaff,Manager")]
        public async Task<IActionResult> RejectOrder(Guid id)
        {
            try
            {
                await this.orderService.RejectOrderAsync(id);

                return Ok("Pedido rejeitado com sucesso");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("{id}/cancel")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> CancelOrder(Guid id, [FromBody] string reason)
        {
            try
            {
                if (!User.IsInRole("Customer"))
                {
                    return Forbid();
                }

                await this.orderService.CancelOrderAsync(id, reason);

                return Ok("Pedido cancelado com sucesso");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
