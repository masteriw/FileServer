using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading.Tasks;

public class SimpleJwtMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SimpleJwtMiddleware> _logger;

    public SimpleJwtMiddleware(RequestDelegate next, ILogger<SimpleJwtMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();

        if (token == null)
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("Token não fornecido.");
            return;
        }

        var tokenHandler = new JwtSecurityTokenHandler();
        try
        {
            var jwtToken = tokenHandler.ReadJwtToken(token);

            // Verificar se o token possui as propriedades básicas esperadas
            if (jwtToken == null || !jwtToken.Claims.Any())
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Token inválido ou expirado.");
                return;
            }

            await _next(context);
        }
        catch (Exception ex)
        {
            context.Response.StatusCode = 401;
            _logger.LogWarning("Token inválido ou expirado. Detalhes: " + ex.Message);
            await context.Response.WriteAsync("Token inválido ou expirado.");
        }
    }
}
