using System.ComponentModel.DataAnnotations;

namespace TPFinal.Api.Entidades;

// representa el producto y sus valores actuales de stock unidad y precio
public class Producto
{
    [Key]
    public string Codigo { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;

    public decimal Stock { get; set; }
    public decimal Stock_Minimo { get; set; }
    public string Unidad { get; set; } = string.Empty;
    public decimal Precio { get; set; }

    public DateTime Fecha_Alta { get; set; }
    public DateTime? Fecha_Baja { get; set; }
}
