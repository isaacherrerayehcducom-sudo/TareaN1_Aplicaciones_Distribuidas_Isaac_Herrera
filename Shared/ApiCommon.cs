using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ComercioMultibodega.Shared;

public interface IRecord
{
    long Id { get; set; }
}

[ApiController]
[Route("api/[controller]")]
public abstract class CrudController<TEntity, TContext>(TContext db) : ControllerBase
    where TEntity : class, IRecord
    where TContext : DbContext
{
    protected TContext Db { get; } = db;
    protected DbSet<TEntity> Items => Db.Set<TEntity>();

    [HttpGet]
    public virtual async Task<ActionResult<IReadOnlyList<TEntity>>> GetAll(CancellationToken ct) =>
        Ok(await Items.AsNoTracking().ToListAsync(ct));

    [HttpGet("{id:long}")]
    public virtual async Task<ActionResult<TEntity>> GetById(long id, CancellationToken ct)
    {
        var item = await Items.FindAsync([id], ct);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public virtual async Task<ActionResult<TEntity>> Create(TEntity input, CancellationToken ct)
    {
        input.Id = 0;
        Items.Add(input);
        try { await Db.SaveChangesAsync(ct); }
        catch (DbUpdateException) { return Conflict(new { message = "El registro viola una restriccion de integridad o unicidad." }); }
        return CreatedAtAction(nameof(GetById), new { id = input.Id }, input);
    }

    [HttpPut("{id:long}")]
    public virtual async Task<IActionResult> Update(long id, TEntity input, CancellationToken ct)
    {
        var current = await Items.FindAsync([id], ct);
        if (current is null) return NotFound();
        input.Id = id;
        Db.Entry(current).CurrentValues.SetValues(input);
        try { await Db.SaveChangesAsync(ct); }
        catch (DbUpdateException) { return Conflict(new { message = "El cambio viola una restriccion de integridad o unicidad." }); }
        return NoContent();
    }

    [HttpDelete("{id:long}")]
    public virtual async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        var item = await Items.FindAsync([id], ct);
        if (item is null) return NotFound();
        Items.Remove(item);
        try { await Db.SaveChangesAsync(ct); }
        catch (DbUpdateException) { return Conflict(new { message = "No se puede eliminar un registro que tiene dependencias." }); }
        return NoContent();
    }
}

public sealed record ProductInfo(long Id, string Nombre, string SKU, decimal Precio, string Estado);

public sealed class ProductCatalogClient(HttpClient http)
{
    public async Task<ProductInfo?> GetActiveProduct(long id, CancellationToken ct)
    {
        using var response = await http.GetAsync($"api/productos/{id}", ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        var product = await response.Content.ReadFromJsonAsync<ProductInfo>(cancellationToken: ct);
        return product is not null && product.Estado == "ACTIVO" ? product : null;
    }
}

public static class ValidationResultExtensions
{
    public static Dictionary<string, string[]> ToErrorDictionary(this IEnumerable<ValidationResult> results) =>
        results.SelectMany(r => (r.MemberNames.DefaultIfEmpty("request")).Select(name => (name, r.ErrorMessage ?? "Valor invalido.")))
            .GroupBy(x => x.name)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Item2).ToArray());
}

