using System.ComponentModel.DataAnnotations;

namespace TPFinal.Api.Entidades;

// representa los datos del cliente identificado por su cuit
public class Cliente
{
    [Key]
    public string CUIT { get; set; } = string.Empty;
    public string RazonSocial { get; set; } = string.Empty;
    public string Domicilio { get; set; } = string.Empty;
    public string Localidad { get; set; } = string.Empty;
    public string CP { get; set; } = string.Empty;

    public string? Celular { get; set; }
    public string? Email { get; set; }

    public DateTime Fecha_Alta { get; set; }
    public DateTime? Fecha_Baja { get; set; }
}
