using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

// Adiciona suporte a Razor Pages
builder.Services.AddRazorPages();

var app = builder.Build();

// Configuração de middleware
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

// Redireciona a raiz "/" para a página Razor desejada
app.Use(async (context, next) =>
{
    if (string.IsNullOrEmpty(context.Request.Path.Value) || context.Request.Path == "/")
    {
        context.Response.Redirect("/Shared/Login/Index");
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

// Inicia o aplicativo
app.Run();
