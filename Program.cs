using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

// ✅ Adiciona suporte a Razor Pages
builder.Services.AddRazorPages();

// ✅ Adiciona suporte a sessão
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(1); // tempo de expiração
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// ✅ Permite injetar HttpContext em classes
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// ✅ Redireciona a raiz "/" para a página de login
app.Use(async (context, next) =>
{
    if (string.IsNullOrEmpty(context.Request.Path.Value) || context.Request.Path == "/")
    {
        context.Response.Redirect("/Shared/Login/Index");
        return;
    }
    await next();
});

// ✅ Configura arquivos estáticos
var fileProvider = new PhysicalFileProvider(
    Path.Combine(Directory.GetCurrentDirectory(), ""));
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = fileProvider,
    RequestPath = ""
});

// ✅ Ativa o uso de sessão (ESSENCIAL)
app.UseSession();

// ✅ Ativa roteamento e autorização
app.UseRouting();
app.UseAuthorization();

// ✅ Mapeia as páginas Razor
app.MapRazorPages();

// ✅ Executa o aplicativo
app.Run();
