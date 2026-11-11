using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using ComercioMultibodega.Shared;

namespace Catalogo.Api;

[Table("Categoria")]
public sealed class Categoria : IRecord
{
    [Key, Column("IdCategoria")] public long Id { get; set; }
    [Required, StringLength(100), Column("Nombre")] public string Nombre { get; set; } = "";
    [StringLength(300), Column("Descripcion")] public string? Descripcion { get; set; }
}

[Table("Marca")]
public sealed class Marca : IRecord
{
    [Key, Column("IdMarca")] public long Id { get; set; }
    [Required, StringLength(100), Column("Nombre")] public string Nombre { get; set; } = "";
    [StringLength(80), Column("PaisOrigen")] public string? PaisOrigen { get; set; }
}

[Table("Producto")]
public sealed class Producto : IRecord
{
    [Key, Column("IdProducto")] public long Id { get; set; }
    [Required, Column("IdCategoria")] public long IdCategoria { get; set; }
    [Required, Column("IdMarca")] public long IdMarca { get; set; }
    [Required, StringLength(160), Column("Nombre")] public string Nombre { get; set; } = "";
    [Required, StringLength(40), Column("SKU")] public string SKU { get; set; } = "";
    [Range(typeof(decimal), "0", "9999999999.99", ParseLimitsInInvariantCulture = true), Column("Precio", TypeName="decimal(12,2)")] public decimal Precio { get; set; }
    [Required, RegularExpression("^(ACTIVO|INACTIVO)$"), StringLength(15), Column("Estado")] public string Estado { get; set; } = "ACTIVO";
    [Column("FechaCreacion")] public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    [ForeignKey(nameof(IdCategoria)), JsonIgnore] public Categoria? Categoria { get; set; }
    [ForeignKey(nameof(IdMarca)), JsonIgnore] public Marca? Marca { get; set; }
}

public sealed class CatalogoDbContext(DbContextOptions<CatalogoDbContext> options) : DbContext(options)
{
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Marca> Marcas => Set<Marca>();
    public DbSet<Producto> Productos => Set<Producto>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<Categoria>().HasIndex(x => x.Nombre).IsUnique();
        model.Entity<Marca>().HasIndex(x => x.Nombre).IsUnique();
        model.Entity<Producto>().HasIndex(x => x.SKU).IsUnique();
        model.Entity<Producto>().HasIndex(x => x.IdCategoria);
        model.Entity<Producto>().HasIndex(x => x.IdMarca);
        model.Entity<Producto>().HasOne(x => x.Categoria).WithMany().HasForeignKey(x => x.IdCategoria).OnDelete(DeleteBehavior.Restrict);
        model.Entity<Producto>().HasOne(x => x.Marca).WithMany().HasForeignKey(x => x.IdMarca).OnDelete(DeleteBehavior.Restrict);
        model.Entity<Producto>().Property(x => x.Precio).HasPrecision(12, 2);
    }
}
