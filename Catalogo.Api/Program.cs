using Catalogo.Api;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
ConfigureDatabase(builder.Services, builder.Configuration);
var app = builder.Build();
app.MapGet("/health", () => Results.Ok(new { servicio = "Catalogo.Api", estado = "ok" }));
app.MapControllers();
await SeedDemoData(app);
app.Run();

static void ConfigureDatabase(IServiceCollection services, IConfiguration config)
{
    var provider = config["Storage:Provider"] ?? "InMemory";
    services.AddDbContext<CatalogoDbContext>(options =>
    {
        if (provider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
            options.UseSqlServer(config.GetConnectionString("Default") ?? throw new InvalidOperationException("Falta ConnectionStrings:Default."));
        else options.UseInMemoryDatabase("CatalogoDB-Demo");
    });
}

static async Task SeedDemoData(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<CatalogoDbContext>();
    if (app.Configuration["Storage:Provider"]?.Equals("SqlServer", StringComparison.OrdinalIgnoreCase) == true || await db.Categorias.AnyAsync()) return;
    db.Categorias.AddRange(new Categoria { Id = 1, Nombre = "Computacion", Descripcion = "Equipos y accesorios" }, new Categoria { Id = 2, Nombre = "Redes", Descripcion = "Conectividad" });
    db.Marcas.AddRange(new Marca { Id = 1, Nombre = "AndesTech", PaisOrigen = "Ecuador" }, new Marca { Id = 2, Nombre = "NetSur", PaisOrigen = "Ecuador" });
    db.Productos.AddRange(new Producto { Id = 1, IdCategoria = 1, IdMarca = 1, Nombre = "Laptop Andina 14", SKU = "AT-LAP-014", Precio = 729.99m }, new Producto { Id = 2, IdCategoria = 2, IdMarca = 2, Nombre = "Router NetSur AX", SKU = "NS-ROU-AX1", Precio = 89.50m });
    await db.SaveChangesAsync();
}
