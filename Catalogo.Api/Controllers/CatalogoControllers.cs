using ComercioMultibodega.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Catalogo.Api.Controllers;

public sealed class CategoriasController(CatalogoDbContext db) : CrudController<Categoria, CatalogoDbContext>(db) { }
public sealed class MarcasController(CatalogoDbContext db) : CrudController<Marca, CatalogoDbContext>(db) { }

[ApiController]
[Route("api/[controller]")]
public sealed class ProductosController : CrudController<Producto, CatalogoDbContext>
{
    private readonly CatalogoDbContext db;

    public ProductosController(CatalogoDbContext db) : base(db) => this.db = db;

    [HttpGet("buscar/{sku}")]
    public async Task<ActionResult<Producto>> GetBySku(string sku, CancellationToken ct)
    {
        var product = await db.Productos.AsNoTracking().SingleOrDefaultAsync(x => x.SKU == sku, ct);
        return product is null ? NotFound() : Ok(product);
    }
}
