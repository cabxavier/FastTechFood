using FastTechFood.Application.Dtos;
using FastTechFood.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FastTechFood.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : Controller
    {
        private readonly IUserService userService;

        public AuthController(IUserService userService)
        {
            this.userService = userService;
        }

        [HttpPost("register/customer")]
        public async Task<IActionResult> RegisterCustomer([FromBody] RegisterCustomerDTO registerCustomerDTO)
        {
            try
            {
                if (registerCustomerDTO == null)
                {
                    return BadRequest("Informe os dados do cliente");
                }

                await this.userService.RegisterCustomerAsync(registerCustomerDTO);

                return Ok("Cliente registrado com sucesso");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("register/employee")]
        public async Task<IActionResult> RegisterEmployee([FromBody] RegisterEmployeeDTO registerEmployeeDTO)
        {
            try
            {
                if (registerEmployeeDTO == null)
                {
                    return BadRequest("Informe os dados do funcionário");
                }

                await this.userService.RegisterEmployeeAsync(registerEmployeeDTO);

                return Ok("Funcionário registrado com sucesso");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Login([FromBody] LoginDTO loginDTO)
        {
            try
            {
                if (loginDTO == null)
                {
                    return BadRequest("Informe os dados do login");
                }

                if (string.IsNullOrWhiteSpace(loginDTO.Email))
                {
                    return BadRequest("E-mail é obrigatório");
                }

                if (string.IsNullOrWhiteSpace(loginDTO.Password))
                {
                    return BadRequest("Password é obrigatório");
                }

                return Ok(new AuthDTO(await this.userService.LoginAsync(loginDTO)));
            }
            catch (Exception ex)
            {
                return Unauthorized(ex.Message);
            }
        }
    }
}
