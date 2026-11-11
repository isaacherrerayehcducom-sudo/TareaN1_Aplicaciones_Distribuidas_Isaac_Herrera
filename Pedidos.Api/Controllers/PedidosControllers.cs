using ComercioMultibodega.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Pedidos.Api.Controllers;

public sealed class ClientesController(PedidosDbContext db) : CrudController<Cliente, PedidosDbContext>(db) { }

public sealed record LineaPedidoRequest(long IdProducto, int Cantidad);
public sealed record CrearPedidoRequest(long IdCliente, List<LineaPedidoRequest> Detalles);

[ApiController]
[Route("api/[controller]")]
public sealed class PedidosController(PedidosDbContext db, ProductCatalogClient catalog) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct) => Ok(await db.Pedidos.AsNoTracking().Include(x => x.Detalles).OrderByDescending(x => x.Fecha).ToListAsync(ct));

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        var order = await db.Pedidos.AsNoTracking().Include(x => x.Detalles).SingleOrDefaultAsync(x => x.Id == id, ct);
        return order is null ? NotFound() : Ok(order);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CrearPedidoRequest request, CancellationToken ct)
    {
        if (request.IdCliente <= 0 || request.Detalles is null || request.Detalles.Count == 0 || request.Detalles.Any(x => x.IdProducto <= 0 || x.Cantidad <= 0))
            return BadRequest(new { message = "El pedido requiere un cliente y al menos un detalle con producto y cantidad validos." });
        if (!await db.Clientes.AnyAsync(x => x.Id == request.IdCliente && x.Estado == "ACTIVO", ct)) return BadRequest(new { message = "El cliente no existe o esta inactivo." });

        var details = new List<DetallePedido>();
        foreach (var line in request.Detalles)
        {
            ProductInfo? product;
            try { product = await catalog.GetActiveProduct(line.IdProducto, ct); }
            catch (HttpRequestException) { return StatusCode(503, new { message = "Catalogo.Api no esta disponible para validar los productos." }); }
            if (product is null) return BadRequest(new { message = $"El producto {line.IdProducto} no existe o esta inactivo." });
            details.Add(new DetallePedido { IdProducto = product.Id, Cantidad = line.Cantidad, PrecioUnitario = product.Precio });
        }

        var total = details.Sum(x => x.Cantidad * x.PrecioUnitario);
        if (total > 999999999999.99m) return BadRequest(new { message = "El total excede el limite permitido." });
        var order = new Pedido { IdCliente = request.IdCliente, Total = total, Estado = "PENDIENTE", Detalles = details };
        db.Pedidos.Add(order);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
    }
}
