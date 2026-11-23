using Microsoft.EntityFrameworkCore;
using TPFinal.Api.Entidades;

namespace TPFinal.Api.Datos;

public class TpDbContext : DbContext
{
    public TpDbContext(DbContextOptions<TpDbContext> opciones)
        : base(opciones)
    {
    }

    // expone las tablas que consultan y modifican los endpoints
    public DbSet<Cliente> Cliente { get; set; }
    public DbSet<Producto> Producto { get; set; }
    public DbSet<PedidoCabecera> Pedidos_Cabecera { get; set; }
    public DbSet<PedidoDetalle> Pedidos_Detalle { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // vincula cada entidad con su tabla y define las claves principales
        modelBuilder.Entity<Cliente>()
            .ToTable("CLIENTE", "dbo")
            .HasKey(c => c.CUIT);

        modelBuilder.Entity<Producto>()
            .ToTable("PRODUCTOS", "dbo")
            .HasKey(p => p.Codigo);

        modelBuilder.Entity<PedidoCabecera>()
            .ToTable("PEDIDOS_CABECERA", "dbo")
            .HasKey(p => p.ID_Pedido);

        modelBuilder.Entity<PedidoDetalle>()
            .ToTable("PEDIDOS_DETALLE", "dbo")
            .HasKey(d => new
            {
                d.ID_Pedido,
                d.Codigo_Producto
            });

        // refleja los tipos y longitudes de las columnas existentes sin cambiar la base
        var cliente = modelBuilder.Entity<Cliente>();
        cliente.Property(c => c.CUIT).IsUnicode(false).HasMaxLength(11);
        cliente.Property(c => c.RazonSocial).IsUnicode(false).HasMaxLength(60);
        cliente.Property(c => c.Domicilio).IsUnicode(false).HasMaxLength(60);
        cliente.Property(c => c.Localidad).IsUnicode(false).HasMaxLength(40);
        cliente.Property(c => c.CP).IsUnicode(false).HasMaxLength(10);
        cliente.Property(c => c.Celular).IsUnicode(false).HasMaxLength(12);
        cliente.Property(c => c.Email).IsUnicode(false).HasMaxLength(100);
        cliente.Property(c => c.Fecha_Alta).HasColumnType("datetime2(7)");
        cliente.Property(c => c.Fecha_Baja).HasColumnType("datetime2(7)");

        // conserva dos decimales para importes y existencias y usa fechas sin hora en productos
        var producto = modelBuilder.Entity<Producto>();
        producto.Property(p => p.Codigo).IsUnicode(false).HasMaxLength(30);
        producto.Property(p => p.Descripcion).IsUnicode(false).HasMaxLength(50);
        producto.Property(p => p.Unidad).IsUnicode(false).HasMaxLength(10);
        producto.Property(p => p.Stock).HasPrecision(10, 2);
        producto.Property(p => p.Stock_Minimo).HasPrecision(10, 2);
        producto.Property(p => p.Precio).HasPrecision(10, 2);
        producto.Property(p => p.Fecha_Alta).HasColumnType("date");
        producto.Property(p => p.Fecha_Baja).HasColumnType("date");

        var cabecera = modelBuilder.Entity<PedidoCabecera>();
        // El número lo calcula el programa original, no EF.
        cabecera.Property(p => p.ID_Pedido).HasColumnName("ID_PEDIDO").ValueGeneratedNever();
        cabecera.Property(p => p.Fecha_Hora).HasColumnType("datetime2(7)");
        cabecera.Property(p => p.CUIT).IsUnicode(false).HasMaxLength(11);
        cabecera.Property(p => p.Operador).IsUnicode(false).HasMaxLength(20);
        cabecera.Property(p => p.Estado).IsUnicode(false).HasMaxLength(20);
        // declara la referencia al cliente sin eliminar automaticamente sus pedidos
        cabecera.HasOne<Cliente>().WithMany().HasForeignKey(p => p.CUIT)
            .OnDelete(DeleteBehavior.NoAction);

        var detalle = modelBuilder.Entity<PedidoDetalle>();
        detalle.Property(d => d.ID_Pedido).HasColumnName("ID_PEDIDO");
        detalle.Property(d => d.Codigo_Producto).IsUnicode(false).HasMaxLength(30);
        detalle.Property(d => d.Unidad).IsUnicode(false).HasMaxLength(10);
        detalle.Property(d => d.Cantidad).HasPrecision(10, 2);
        detalle.Property(d => d.Precio).HasPrecision(10, 2);
        // permite ordenar las inserciones de cabecera y detalle respetando las claves foraneas
        detalle.HasOne<PedidoCabecera>().WithMany().HasForeignKey(d => d.ID_Pedido)
            .OnDelete(DeleteBehavior.NoAction);
        detalle.HasOne<Producto>().WithMany().HasForeignKey(d => d.Codigo_Producto)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
