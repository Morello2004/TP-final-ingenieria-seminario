namespace TPFinal.Api.Entidades;

// conserva la cantidad unidad y precio del producto al registrar el pedido
public class PedidoDetalle
{
    public int ID_Pedido { get; set; }
    public string Codigo_Producto { get; set; } = string.Empty;

    public decimal Cantidad { get; set; }
    public string Unidad { get; set; } = string.Empty;
    public decimal Precio { get; set; }
}
