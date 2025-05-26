using Microsoft.IdentityModel.Tokens;
using System.Net;
using System.Text.Json;

namespace FastTechFood.API.Middlewares
{
    public class JwtExceptionHandlerMiddleware
    {
        private readonly RequestDelegate requestDelegate;
        private readonly ILogger<JwtExceptionHandlerMiddleware> logger;

        public JwtExceptionHandlerMiddleware(RequestDelegate requestDelegate, ILogger<JwtExceptionHandlerMiddleware> logger)
        {
            this.requestDelegate = requestDelegate;
            this.logger = logger;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            try
            {
                await this.requestDelegate(httpContext);
            }
            catch (SecurityTokenExpiredException)
            {
                httpContext.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                httpContext.Response.ContentType = "application/json";
                await httpContext.Response.WriteAsync(JsonSerializer.Serialize(new
                {
                    httpContext.Response.StatusCode,
                    Message = "Token expirado"

                }));
            }
            catch (SecurityTokenValidationException)
            {
                httpContext.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                httpContext.Response.ContentType = "application/json";
                await httpContext.Response.WriteAsync(JsonSerializer.Serialize(new
                {
                    httpContext.Response.StatusCode,
                    Message = "Token inválido"

                }));
            }
        }
    }
}
