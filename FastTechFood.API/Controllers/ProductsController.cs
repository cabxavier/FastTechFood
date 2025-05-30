using FastTechFood.Application.Dtos;
using FastTechFood.Application.Interfaces;
using FastTechFood.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FastTechFood.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : Controller
    {
        private readonly IProductService productService;

        public ProductsController(IProductService productService)
        {
            this.productService = productService;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await this.productService.GetAllProductsAsync());
        }

        [HttpGet("type/{productType}")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> GetByType(ProductType productType)
        {
            return Ok(await this.productService.GetProductsByTypeAsync(productType));
        }

        [HttpPost]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> Create([FromBody] ProductDTO productDTO)
        {
            try
            {
                await this.productService.AddProductAsync(productDTO);

                return Ok("Produto criado com sucesso");
            }
            catch(Exception ex)
            {
                    return BadRequest(ex.Message);
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> Update(Guid id, [FromBody] ProductDTO productDTO)
        {
            try
            {
                await this.productService.UpdateProductAsync(id, productDTO);

                return Ok("Produto atualizdo com sucesso");
            }
            catch(Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
