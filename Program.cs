using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

var secretKey = "LPCS-JER_kw63ozyKYsbpOvDfkb6iiZ6cx9UQFu6fKDWH2ECRIDODdwNTT_oiR3rY9VSkZPpOdY94Zv78OmuhTK9AENutBuSsLHkrNWU7PHVe7mvuzY3iYhsJ369SyN83RyZQBYS4axb1trRqSCJ3N3DOK9r6Y8LL19os3ZryxmO8igWeng3XynLDFOBxC9aOW0_nCUhiY1dEiwxpVTjOe5u_v7Ain4ihyO28uUwNEOc9drn2WRvklCT7U9mXWx1L7sRuP6_doRETKDX_ZSL6oVuBVa7KgZ9S9BuEBd6uMs-NjqZZ6dpVBpKHA5LRcX1mlB7ecPFq_ouwtJeAPpMBI8b0UaSGNA_mBqfOn7O95bzPR0SbYepAoQUmvu9jiDJf0Gm_LDjdue6LrLcNJgxRAXFq0AcLvtejAn3Zdftj85Vn5h3bO8c-F8z8m5pVtYgCZ7d_G9PEMpx9NkVNZucgrOd1ezMFwZDvDqPhUnY2AK5trG_ktT0uA9xSVmU70uIGcu_boM4dBBiCtIIeB7ZNXUyex0WNX9wztjJw3SPnYISki9xxG-zGu2EcGIr0GuijNTW34-2s4I2k8AX_mX5Bkszsv4d88gqDxXsSowjieXO3sJhJ5d1vOfKfI2UOGR_MzgLUXAZSUW8SnrwhSP3rIduQ7nxdORiXoPsf9ESCk95c9XKoZF6G7StOxY2NgWoIu8YY0HU-wGpcRB9BvdOYl4cOqO5PscjIWsoRfaQZNpSn1oDvuNw9G580Bt_O2Y707xwFkiPTQbAUygPhll6pJfLF2hKzCoySTqeclrgm8nb1DfYRcDIUIRe6YXiRC-_maUOLGlWIpnEuXBj_N5Jpfw2mrw2W7Q5Nq65xePvSJB5YqbyIDNj3Sd78pJTK54HkMUUHyx0jTUSJUP1JfA0bEOmpGOVAj-Y6yN3rCEEc9Dil9ZqKIfVtuIQxqYA2g5D";
var keyBytes = Encoding.UTF8.GetBytes(secretKey);
var signingKey = new SymmetricSecurityKey(keyBytes);

// Configuração do token JWT
//var tokenValidationParameters = new TokenValidationParameters
//{
//    ValidateIssuer = false,
//    ValidateAudience = false,
//    ValidateLifetime = false, // Validação do tempo de expiração
//    ValidateIssuerSigningKey = false, // Validaremos a chave secreta
//    IssuerSigningKey = signingKey,
//    ClockSkew = TimeSpan.FromMinutes(500000) // Evita a tolerância de tempo (tokens expirados são rejeitados)
//};
var configPath = "appsettings.json"; 
var json = File.ReadAllText(configPath);
var config = JObject.Parse(json);
var issuer = config["issuer"].ToString();



var tokenValidationParameters = new TokenValidationParameters
{
    ValidateIssuer = true,
    ValidIssuer = "meuSegredo",

    ValidateAudience = false,
    ValidAudience = "",

    ValidateIssuerSigningKey = false,

    SignatureValidator = delegate (string token, TokenValidationParameters parameters)
    {
        var jwt = new JwtSecurityToken(token);

        return jwt;
    },

    RequireExpirationTime = false,
    ValidateLifetime = false,

    ClockSkew = TimeSpan.Zero,
};

tokenValidationParameters.RequireSignedTokens = false;

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = tokenValidationParameters;
    });

// Adicione serviços de páginas Razor
builder.Services.AddRazorPages();

// Configuração do CORS para permitir qualquer domínio
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAnyOrigin", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

// Configure o Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("error.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

Log.Information("Application started!");
Log.Error("This is a test error message!");

var app = builder.Build();

// Configure o pipeline de requisições HTTP.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseCors("AllowAnyOrigin"); // Aplica a política CORS

app.UseAuthentication(); // Adicione o middleware de autenticação
app.UseAuthorization(); // Adicione o middleware de autorização

app.MapPost("/StaticFiles", async context =>
{
    try
    {
        // Verifique se há algum texto no parâmetro "fileName"
        var fileName = context.Request.Form["fileName"];
        if (!string.IsNullOrEmpty(fileName))
        {
            // Verifique o token
            var token = context.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out _);

            if (principal.Identity.IsAuthenticated)
            {
                // Lógica para servir o arquivo do diretório MyStaticFiles
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "MyStaticFiles", fileName);
                if (File.Exists(filePath))
                {
                    Log.Information(("Arquivo " + filePath + " enviado com sucesso."));
                    await context.Response.SendFileAsync(filePath);
                }
                else
                {
                    context.Response.StatusCode = 404;
                    Log.Information(("Arquivo " + filePath + " não encontrado."));
                    await context.Response.WriteAsync("Arquivo não encontrado.");
                }
            }
            else
            {
                context.Response.StatusCode = 401; // Não autorizado
                Log.Information("Token" + token + "inválido ou expirado.");
                await context.Response.WriteAsync("Token inválido ou expirado.");
            }
        }
        else
        {
            context.Response.StatusCode = 400;
            Log.Information("Parâmetro 'fileName' não especificado.");
            await context.Response.WriteAsync("Parâmetro 'fileName' não especificado.");
        }
    }
    catch(Exception ex)
    {
        context.Response.StatusCode = 400;
        Log.Error(ex.Message);
        Log.Error("Token request info: " + context.Request.Headers["Authorization"]);
        Log.Error("Request: " + context.Request.Form["fileName"]);
        await context.Response.WriteAsync("Erro processando token: " + ex.Message);
    }
});

app.MapRazorPages();

app.Run();
