using System.Data;
using ComercioMultibodega.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Inventario.Api.Controllers;

public sealed class BodegasController(InventarioDbContext db) : CrudController<Bodega, InventarioDbContext>(db) { }

[ApiController]
[Route("api/[controller]")]
public sealed class ExistenciasController(InventarioDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] long? idProducto, [FromQuery] long? idBodega, CancellationToken ct)
    {
        var query = db.Existencias.AsNoTracking().AsQueryable();
        if (idProducto.HasValue) query = query.Where(x => x.IdProducto == idProducto.Value);
        if (idBodega.HasValue) query = query.Where(x => x.IdBodega == idBodega.Value);
        return Ok(await query.OrderBy(x => x.IdProducto).ThenBy(x => x.IdBodega).ToListAsync(ct));
    }
}

public sealed record MovimientoRequest(long IdProducto, long IdBodega, string Tipo, int Cantidad, string? Observacion);

[ApiController]
[Route("api/[controller]")]
public sealed class MovimientosController(InventarioDbContext db, ProductCatalogClient catalog) : ControllerBase
{
    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        var movement = await db.Movimientos.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
        return movement is null ? NotFound() : Ok(movement);
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] long? idProducto, [FromQuery] long? idBodega, CancellationToken ct)
    {
        var query = db.Movimientos.AsNoTracking().AsQueryable();
        if (idProducto.HasValue) query = query.Where(x => x.IdProducto == idProducto.Value);
        if (idBodega.HasValue) query = query.Where(x => x.IdBodega == idBodega.Value);
        return Ok(await query.OrderByDescending(x => x.Fecha).ToListAsync(ct));
    }

    [HttpPost]
    public async Task<IActionResult> Create(MovimientoRequest request, CancellationToken ct)
    {
        if (request.IdProducto <= 0 || request.IdBodega <= 0 || request.Cantidad <= 0 || request.Tipo is not ("ENTRADA" or "SALIDA"))
            return BadRequest(new { message = "Producto, bodega y cantidad deben ser validos; Tipo debe ser ENTRADA o SALIDA." });
        try
        {
            if (await catalog.GetActiveProduct(request.IdProducto, ct) is null) return BadRequest(new { message = "El producto no existe o esta inactivo en Catalogo.Api." });
        }
        catch (HttpRequestException) { return StatusCode(503, new { message = "Catalogo.Api no esta disponible para validar el producto." }); }

        if (!await db.Bodegas.AnyAsync(x => x.Id == request.IdBodega && x.Estado == "ACTIVA", ct)) return BadRequest(new { message = "La bodega no existe o esta inactiva." });
        await using var transaction = db.Database.IsRelational() ? await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct) : null;
        var stock = await db.Existencias.SingleOrDefaultAsync(x => x.IdProducto == request.IdProducto && x.IdBodega == request.IdBodega, ct);
        if (stock is null && request.Tipo == "SALIDA") return Conflict(new { message = "No hay existencia para este producto en la bodega." });
        stock ??= new Existencia { IdProducto = request.IdProducto, IdBodega = request.IdBodega, Stock = 0 };
        var reserved = await db.Reservas.Where(x => x.IdProducto == request.IdProducto && x.IdBodega == request.IdBodega && x.Estado == "ACTIVA").SumAsync(x => (int?)x.Cantidad, ct) ?? 0;
        var nextStock = request.Tipo == "ENTRADA" ? stock.Stock + request.Cantidad : stock.Stock - request.Cantidad;
        if (nextStock < reserved) return Conflict(new { message = "La salida supera el stock disponible despues de reservas activas.", stock = stock.Stock, reservado = reserved });
        stock.Stock = nextStock;
        stock.FechaActualizacion = DateTime.UtcNow;
        if (stock.Id == 0) db.Existencias.Add(stock);
        var movement = new MovimientoInventario { IdProducto = request.IdProducto, IdBodega = request.IdBodega, Tipo = request.Tipo, Cantidad = request.Cantidad, Observacion = request.Observacion };
        db.Movimientos.Add(movement);
        await db.SaveChangesAsync(ct);
        if (transaction is not null) await transaction.CommitAsync(ct);
        return Created($"/api/movimientos/{movement.Id}", new { movimiento = movement, stock = stock.Stock });
    }
}

public sealed record ReservaRequest(long IdProducto, long IdBodega, int Cantidad);

[ApiController]
[Route("api/[controller]")]
public sealed class ReservasController(InventarioDbContext db, ProductCatalogClient catalog) : ControllerBase
{
    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        var reservation = await db.Reservas.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
        return reservation is null ? NotFound() : Ok(reservation);
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string? estado, CancellationToken ct)
    {
        var query = db.Reservas.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(estado)) query = query.Where(x => x.Estado == estado);
        return Ok(await query.OrderByDescending(x => x.Fecha).ToListAsync(ct));
    }

    [HttpPost]
    public async Task<IActionResult> Create(ReservaRequest request, CancellationToken ct)
    {
        if (request.IdProducto <= 0 || request.IdBodega <= 0 || request.Cantidad <= 0) return BadRequest(new { message = "Producto, bodega y cantidad deben ser mayores que cero." });
        try
        {
            if (await catalog.GetActiveProduct(request.IdProducto, ct) is null) return BadRequest(new { message = "El producto no existe o esta inactivo en Catalogo.Api." });
        }
        catch (HttpRequestException) { return StatusCode(503, new { message = "Catalogo.Api no esta disponible para validar el producto." }); }
        if (!await db.Bodegas.AnyAsync(x => x.Id == request.IdBodega && x.Estado == "ACTIVA", ct)) return BadRequest(new { message = "La bodega no existe o esta inactiva." });

        await using var transaction = db.Database.IsRelational() ? await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct) : null;
        var stock = await db.Existencias.SingleOrDefaultAsync(x => x.IdProducto == request.IdProducto && x.IdBodega == request.IdBodega, ct);
        var active = await db.Reservas.Where(x => x.IdProducto == request.IdProducto && x.IdBodega == request.IdBodega && x.Estado == "ACTIVA").SumAsync(x => (int?)x.Cantidad, ct) ?? 0;
        if (stock is null || stock.Stock - active < request.Cantidad) return Conflict(new { message = "Stock insuficiente para crear la reserva.", stock = stock?.Stock ?? 0, reservado = active });
        var reservation = new ReservaStock { IdProducto = request.IdProducto, IdBodega = request.IdBodega, Cantidad = request.Cantidad };
        db.Reservas.Add(reservation);
        await db.SaveChangesAsync(ct);
        if (transaction is not null) await transaction.CommitAsync(ct);
        return Created($"/api/reservas/{reservation.Id}", reservation);
    }

    [HttpPatch("{id:long}/liberar")]
    public async Task<IActionResult> Release(long id, CancellationToken ct)
    {
        var reservation = await db.Reservas.FindAsync([id], ct);
        if (reservation is null) return NotFound();
        if (reservation.Estado != "ACTIVA") return Conflict(new { message = "Solo se pueden liberar reservas activas." });
        reservation.Estado = "LIBERADA";
        await db.SaveChangesAsync(ct);
        return Ok(reservation);
    }
}
