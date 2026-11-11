using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using ComercioMultibodega.Shared;

namespace Inventario.Api;

[Table("Bodega")]
public sealed class Bodega : IRecord
{
    [Key, Column("IdBodega")] public long Id { get; set; }
    [Required, StringLength(120), Column("Nombre")] public string Nombre { get; set; } = "";
    [Required, StringLength(100), Column("Ciudad")] public string Ciudad { get; set; } = "";
    [Required, StringLength(240), Column("Direccion")] public string Direccion { get; set; } = "";
    [Required, RegularExpression("^(ACTIVA|INACTIVA)$"), StringLength(15), Column("Estado")] public string Estado { get; set; } = "ACTIVA";
}

[Table("Existencia")]
public sealed class Existencia : IRecord
{
    [Key, Column("IdExistencia")] public long Id { get; set; }
    [Required, Column("IdProducto")] public long IdProducto { get; set; }
    [Required, Column("IdBodega")] public long IdBodega { get; set; }
    [Range(0, int.MaxValue), Column("Stock")] public int Stock { get; set; }
    [Column("FechaActualizacion")] public DateTime FechaActualizacion { get; set; } = DateTime.UtcNow;
}

[Table("MovimientoInventario")]
public sealed class MovimientoInventario : IRecord
{
    [Key, Column("IdMovimiento")] public long Id { get; set; }
    [Required, Column("IdProducto")] public long IdProducto { get; set; }
    [Required, Column("IdBodega")] public long IdBodega { get; set; }
    [Required, StringLength(12), Column("Tipo")] public string Tipo { get; set; } = "";
    [Range(1, int.MaxValue), Column("Cantidad")] public int Cantidad { get; set; }
    [Column("Fecha")] public DateTime Fecha { get; set; } = DateTime.UtcNow;
    [StringLength(250), Column("Observacion")] public string? Observacion { get; set; }
}

[Table("ReservaStock")]
public sealed class ReservaStock : IRecord
{
    [Key, Column("IdReserva")] public long Id { get; set; }
    [Required, Column("IdProducto")] public long IdProducto { get; set; }
    [Required, Column("IdBodega")] public long IdBodega { get; set; }
    [Range(1, int.MaxValue), Column("Cantidad")] public int Cantidad { get; set; }
    [Required, RegularExpression("^(ACTIVA|INACTIVA|LIBERADA|CONSUMIDA|CANCELADA)$"), StringLength(15), Column("Estado")] public string Estado { get; set; } = "ACTIVA";
    [Column("Fecha")] public DateTime Fecha { get; set; } = DateTime.UtcNow;
}

public sealed class InventarioDbContext(DbContextOptions<InventarioDbContext> options) : DbContext(options)
{
    public DbSet<Bodega> Bodegas => Set<Bodega>();
    public DbSet<Existencia> Existencias => Set<Existencia>();
    public DbSet<MovimientoInventario> Movimientos => Set<MovimientoInventario>();
    public DbSet<ReservaStock> Reservas => Set<ReservaStock>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<Bodega>().HasIndex(x => new { x.Nombre, x.Ciudad }).IsUnique();
        model.Entity<Existencia>().HasIndex(x => new { x.IdProducto, x.IdBodega }).IsUnique();
        model.Entity<Existencia>().HasIndex(x => x.IdProducto);
        model.Entity<Existencia>().HasOne<Bodega>().WithMany().HasForeignKey(x => x.IdBodega).OnDelete(DeleteBehavior.Restrict);
        model.Entity<MovimientoInventario>().HasIndex(x => new { x.IdProducto, x.Fecha });
        model.Entity<MovimientoInventario>().HasOne<Bodega>().WithMany().HasForeignKey(x => x.IdBodega).OnDelete(DeleteBehavior.Restrict);
        model.Entity<ReservaStock>().HasIndex(x => new { x.IdProducto, x.IdBodega, x.Estado });
        model.Entity<ReservaStock>().HasOne<Bodega>().WithMany().HasForeignKey(x => x.IdBodega).OnDelete(DeleteBehavior.Restrict);
    }
}
