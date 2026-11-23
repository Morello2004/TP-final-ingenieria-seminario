using System.ComponentModel.DataAnnotations;

namespace TPFinal.Api.Entidades;

// agrupa los datos generales del pedido cuyo numero calcula la aplicacion
public class PedidoCabecera
{
    [Key]
    public int ID_Pedido { get; set; }

    public DateTime Fecha_Hora { get; set; }
    public string CUIT { get; set; } = string.Empty;
    public string Operador { get; set; } = string.Empty;
    public string Estado { get; set; } = "PENDIENTE";
}
