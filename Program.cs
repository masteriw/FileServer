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

var configPath = "appsettings.json"; 
var json = File.ReadAllText(configPath);
var config = JObject.Parse(json);
var issuer = config["issuer"].ToString();
var secretKey = config["secretKey"].ToString();
var keyBytes = Encoding.UTF8.GetBytes(secretKey);
var signingKey = new SymmetricSecurityKey(keyBytes);


var tokenValidationParameters = new TokenValidationParameters
{
    ValidateIssuer = true,
    ValidIssuer = issuer,

    ValidateAudience = false,
    ValidAudience = "",

    ValidateIssuerSigningKey = true,

    SignatureValidator = delegate (string token, TokenValidationParameters parameters)
    {
        var jwt = new JwtSecurityToken(token);

        return jwt;
    },

    RequireExpirationTime = false,
    ValidateLifetime = false,

    ClockSkew = TimeSpan.Zero,
};

tokenValidationParameters.RequireSignedTokens = !tokenValidationParameters.RequireSignedTokens;

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
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.Console()
    .WriteTo.File("error.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

Log.Information("Application started!");
Log.Error("Essa é uma mensagem de erro de testes, gerada na inicialização do serviço.");

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
                    Log.Error(("Arquivo " + filePath + " não encontrado."));
                    await context.Response.WriteAsync("Arquivo não encontrado.");
                }
            }
            else
            {
                context.Response.StatusCode = 401; // Não autorizado
                Log.Warning("Token" + token + "inválido ou expirado.");
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
