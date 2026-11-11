using ComercioMultibodega.Shared;
using Microsoft.EntityFrameworkCore;
using Pedidos.Api;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddHttpClient<ProductCatalogClient>(client => client.BaseAddress = new Uri(builder.Configuration["Services:CatalogApi"] ?? "http://localhost:5081/"));
ConfigureDatabase(builder.Services, builder.Configuration);
var app = builder.Build();
app.MapGet("/health", () => Results.Ok(new { servicio = "Pedidos.Api", estado = "ok" }));
app.MapControllers();
await SeedDemoData(app);
app.Run();

static void ConfigureDatabase(IServiceCollection services, IConfiguration config)
{
    var provider = config["Storage:Provider"] ?? "InMemory";
    services.AddDbContext<PedidosDbContext>(options =>
    {
        if (provider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
            options.UseSqlServer(config.GetConnectionString("Default") ?? throw new InvalidOperationException("Falta ConnectionStrings:Default."));
        else options.UseInMemoryDatabase("PedidosDB-Demo");
    });
}

static async Task SeedDemoData(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<PedidosDbContext>();
    if (app.Configuration["Storage:Provider"]?.Equals("SqlServer", StringComparison.OrdinalIgnoreCase) == true || await db.Clientes.AnyAsync()) return;
    db.Clientes.Add(new Cliente { Id = 1, Nombres = "Cliente de Prueba", Cedula = "0000000000", Correo = "cliente.prueba@example.test" });
    await db.SaveChangesAsync();
}
