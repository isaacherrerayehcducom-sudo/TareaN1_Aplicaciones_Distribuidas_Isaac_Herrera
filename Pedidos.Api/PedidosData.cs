using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using ComercioMultibodega.Shared;

namespace Pedidos.Api;

[Table("Cliente")]
public sealed class Cliente : IRecord
{
    [Key, Column("IdCliente")] public long Id { get; set; }
    [Required, StringLength(160), Column("Nombres")] public string Nombres { get; set; } = "";
    [Required, StringLength(20), Column("Cedula")] public string Cedula { get; set; } = "";
    [Required, EmailAddress, StringLength(254), Column("Correo")] public string Correo { get; set; } = "";
    [Required, RegularExpression("^(ACTIVO|INACTIVO)$"), StringLength(15), Column("Estado")] public string Estado { get; set; } = "ACTIVO";
}

[Table("Pedido")]
public sealed class Pedido : IRecord
{
    [Key, Column("IdPedido")] public long Id { get; set; }
    [Required, Column("IdCliente")] public long IdCliente { get; set; }
    [Column("Fecha")] public DateTime Fecha { get; set; } = DateTime.UtcNow;
    [Range(typeof(decimal), "0", "999999999999.99", ParseLimitsInInvariantCulture = true), Column("Total", TypeName="decimal(14,2)")] public decimal Total { get; set; }
    [Required, RegularExpression("^(PENDIENTE|CONFIRMADO|RESERVADO|DESPACHADO|CANCELADO)$"), StringLength(15), Column("Estado")] public string Estado { get; set; } = "PENDIENTE";
    [ForeignKey(nameof(IdCliente)), JsonIgnore] public Cliente? Cliente { get; set; }
    public List<DetallePedido> Detalles { get; set; } = [];
}

[Table("DetallePedido")]
public sealed class DetallePedido : IRecord
{
    [Key, Column("IdDetalle")] public long Id { get; set; }
    [Required, Column("IdPedido")] public long IdPedido { get; set; }
    [Required, Column("IdProducto")] public long IdProducto { get; set; }
    [Range(1, int.MaxValue), Column("Cantidad")] public int Cantidad { get; set; }
    [Range(typeof(decimal), "0", "9999999999.99", ParseLimitsInInvariantCulture = true), Column("PrecioUnitario", TypeName="decimal(12,2)")] public decimal PrecioUnitario { get; set; }
    [ForeignKey(nameof(IdPedido)), JsonIgnore] public Pedido? Pedido { get; set; }
}

public sealed class PedidosDbContext(DbContextOptions<PedidosDbContext> options) : DbContext(options)
{
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Pedido> Pedidos => Set<Pedido>();
    public DbSet<DetallePedido> Detalles => Set<DetallePedido>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<Cliente>().HasIndex(x => x.Cedula).IsUnique();
        model.Entity<Cliente>().HasIndex(x => x.Correo).IsUnique();
        model.Entity<Pedido>().HasIndex(x => new { x.IdCliente, x.Fecha });
        model.Entity<Pedido>().Property(x => x.Total).HasPrecision(14, 2);
        model.Entity<Pedido>().HasOne(x => x.Cliente).WithMany().HasForeignKey(x => x.IdCliente).OnDelete(DeleteBehavior.Restrict);
        model.Entity<Pedido>().HasMany(x => x.Detalles).WithOne(x => x.Pedido).HasForeignKey(x => x.IdPedido).OnDelete(DeleteBehavior.Restrict);
        model.Entity<DetallePedido>().Property(x => x.PrecioUnitario).HasPrecision(12, 2);
        model.Entity<DetallePedido>().HasIndex(x => x.IdProducto);
    }
}
