using ComercioMultibodega.Shared;
using Inventario.Api;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddHttpClient<ProductCatalogClient>(client => client.BaseAddress = new Uri(builder.Configuration["Services:CatalogApi"] ?? "http://localhost:5081/"));
ConfigureDatabase(builder.Services, builder.Configuration);
var app = builder.Build();
app.MapGet("/health", () => Results.Ok(new { servicio = "Inventario.Api", estado = "ok" }));
app.MapControllers();
await SeedDemoData(app);
app.Run();

static void ConfigureDatabase(IServiceCollection services, IConfiguration config)
{
    var provider = config["Storage:Provider"] ?? "InMemory";
    services.AddDbContext<InventarioDbContext>(options =>
    {
        if (provider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
            options.UseSqlServer(config.GetConnectionString("Default") ?? throw new InvalidOperationException("Falta ConnectionStrings:Default."));
        else options.UseInMemoryDatabase("InventarioDB-Demo");
    });
}

static async Task SeedDemoData(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<InventarioDbContext>();
    if (app.Configuration["Storage:Provider"]?.Equals("SqlServer", StringComparison.OrdinalIgnoreCase) == true || await db.Bodegas.AnyAsync()) return;
    db.Bodegas.AddRange(new Bodega { Id = 1, Nombre = "Bodega Quito", Ciudad = "Quito", Direccion = "Av. 10 de Agosto", Estado = "ACTIVA" }, new Bodega { Id = 2, Nombre = "Bodega Guayaquil", Ciudad = "Guayaquil", Direccion = "Av. Juan Tanca Marengo", Estado = "ACTIVA" });
    db.Existencias.AddRange(new Existencia { Id = 1, IdProducto = 1, IdBodega = 1, Stock = 25 }, new Existencia { Id = 2, IdProducto = 1, IdBodega = 2, Stock = 12 }, new Existencia { Id = 3, IdProducto = 2, IdBodega = 1, Stock = 40 });
    await db.SaveChangesAsync();
}
