using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

// Adiciona Razor Pages
builder.Services.AddRazorPages();

var app = builder.Build();

// Configuração de middleware
app.UseHttpsRedirection();
app.UseStaticFiles(); // Permite o acesso a arquivos estáticos
app.UseRouting();
app.UseAuthorization();

// Redireciona para o index.html na raiz do projeto
app.Use(async (context, next) =>
{
    if (!context.Request.Path.HasValue || context.Request.Path == "/")
    {
        context.Response.Redirect("/index.html");
        return;
    }
    await next();
});

// Configura o diretório raiz para arquivos estáticos
var fileProvider = new PhysicalFileProvider(
    Path.Combine(Directory.GetCurrentDirectory(), ""));
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = fileProvider,
    RequestPath = ""
});

// Mapeia as páginas Razor
app.MapRazorPages();
app.Run();
