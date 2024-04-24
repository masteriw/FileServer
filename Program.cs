using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

// Configure logging
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.SetMinimumLevel(LogLevel.Debug);

// Add services to the container.
builder.Services.AddRazorPages();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// Add a custom middleware to log requests
app.Use(async (context, next) =>
{
    // Get the logger
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();

    // Log something
    logger.LogDebug("Requisição recebida: {0}", context.Request.Path);

    await next.Invoke();
});

// UseStaticFiles to serve files from the "MyStaticFiles" folder
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(Directory.GetCurrentDirectory(), "MyStaticFiles")),
    RequestPath = "/StaticFiles"
});

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

app.Run();
