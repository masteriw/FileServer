using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.IO;

var builder = WebApplication.CreateBuilder(args);

// Configure logging
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.SetMinimumLevel(LogLevel.Debug);

// Adicione serviços ao contêiner.
builder.Services.AddRazorPages();

var app = builder.Build();

// Configure o pipeline de requisições HTTP.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// Adicione um middleware para lidar com requisições POST em /StaticFiles
app.Use(async (context, next) =>
{
    if (context.Request.Method == "POST" && context.Request.Path == "/StaticFiles")
    {
        // Verifique se há algum texto no parâmetro "fileName"
        var fileName = context.Request.Form["fileName"];
        if (!string.IsNullOrEmpty(fileName))
        {
            // Lógica para servir o arquivo do diretório MyStaticFiles
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "MyStaticFiles", fileName);
            if (File.Exists(filePath))
            {
                await context.Response.SendFileAsync(filePath);
            }
            else
            {
                context.Response.StatusCode = 404;
                await context.Response.WriteAsync("Arquivo não encontrado.");
            }
        }
        else
        {
            context.Response.StatusCode = 400;
            await context.Response.WriteAsync("Parâmetro 'meuParametro' não especificado.");
        }
    }
    else
    {
        // Se não for uma requisição POST para /StaticFiles, continue com o próximo middleware.
        await next.Invoke();
    }
});

app.UseRouting();
app.UseAuthorization();
app.MapRazorPages();

app.Run();
