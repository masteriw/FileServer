using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
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
using Microsoft.Extensions.Options;

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

builder.Services.AddRazorPages();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAnyOrigin", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

// Adicionar o contexto do banco de dados
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.Console()
    .WriteTo.File("error.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

Log.Information("Application started!");
Log.Error("Essa é uma mensagem de erro de testes, gerada na inicialização do serviço.");

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseCors("AllowAnyOrigin");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.MapPost("/StaticFiles", async context =>
{
    try
    {
        var fileName = context.Request.Form["fileName"];
        if (string.IsNullOrEmpty(fileName))
        {
            context.Response.StatusCode = 400;
            Log.Information("Parâmetro 'fileName' não especificado.");
            await context.Response.WriteAsync("Parâmetro 'fileName' não especificado.");
            return;
        }

        var token = context.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
        var tokenHandler = new JwtSecurityTokenHandler();
        ClaimsPrincipal principal;
        try
        {
            principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out _);
        }
        catch (Exception ex)
        {
            context.Response.StatusCode = 401;
            Log.Warning("Token inválido ou expirado. Detalhes: " + ex.Message);
            await context.Response.WriteAsync("Token inválido ou expirado.");
            return;
        }

        if (!principal.Identity.IsAuthenticated)
        {
            context.Response.StatusCode = 401;
            Log.Warning("Token não autenticado.");
            await context.Response.WriteAsync("Token não autenticado.");
            return;
        }

        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "MyStaticFiles", fileName);
        if (!File.Exists(filePath))
        {
            context.Response.StatusCode = 404;
            Log.Error("Arquivo " + filePath + " não encontrado.");
            await context.Response.WriteAsync("Arquivo não encontrado.");
            return;
        }

        Log.Information("Arquivo " + filePath + " enviado com sucesso.");
        await context.Response.SendFileAsync(filePath);
    }
    catch (Exception ex)
    {
        context.Response.StatusCode = 500;
        Log.Error("Erro interno no servidor: " + ex.Message);
        await context.Response.WriteAsync("Erro interno no servidor.");
    }
});

app.MapGet("/files", async (HttpContext context, string agency, AppDbContext db) =>
{
    var token = context.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");

    if (string.IsNullOrEmpty(token))
    {
        context.Response.StatusCode = 401;
        Log.Warning("Token não fornecido.");
        await context.Response.WriteAsync("Token não fornecido.");
        return;
    }

    var tokenHandler = new JwtSecurityTokenHandler();
    ClaimsPrincipal principal;
    try
    {
        principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out _);
    }
    catch (Exception ex)
    {
        context.Response.StatusCode = 401;
        Log.Warning("Token inválido ou expirado. Detalhes: " + ex.Message);
        await context.Response.WriteAsync("Token inválido ou expirado.");
        return;
    }

    if (!principal.Identity.IsAuthenticated)
    {
        context.Response.StatusCode = 401;
        Log.Warning("Token não autenticado.");
        await context.Response.WriteAsync("Token não autenticado.");
        return;
    }

    try
    {
        var files = await db.FileRecords
                            .Where(fr => fr.agency == agency)
                            .Select(fr => fr.filename)
                            .ToListAsync();

        await context.Response.WriteAsJsonAsync(files);
    }
    catch (Exception ex)
    {
        context.Response.StatusCode = 500;
        Log.Error("Erro ao buscar arquivos: " + ex.Message);
        await context.Response.WriteAsync("Erro ao buscar arquivos.");
    }
});

app.MapRazorPages();

app.Run();
