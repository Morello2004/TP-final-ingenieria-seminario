namespace TPFinal.Api.Entidades;

// recibe los datos enviados por el formulario para registrar un pedido
public class NuevoPedido
{
    public string CUIT { get; set; } = string.Empty;
    public string Operador { get; set; } = string.Empty;

    public List<NuevoPedidoDetalle> Detalles { get; set; } = [];
}

// recibe solo el codigo y la cantidad porque el servidor obtiene la unidad y el precio
public class NuevoPedidoDetalle
{
    public string Codigo_Producto { get; set; } = string.Empty;
    public decimal Cantidad { get; set; }
}
