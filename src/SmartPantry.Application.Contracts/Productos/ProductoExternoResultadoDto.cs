using System.Collections.Generic;

namespace SmartPantry.Productos;

public class ProductoExternoResultadoDto
{
    public EstadoConsultaProductoExterno Estado { get; set; }
    public string? CodigoBarras { get; set; }
    public string? Nombre { get; set; }
    public string? Marca { get; set; }
    public string? ImagenUrl { get; set; }
    public List<string>? Ingredientes { get; set; }
    public List<string>? Alergenos { get; set; }
    public string? MensajeError { get; set; }
}

public enum EstadoConsultaProductoExterno
{
    Encontrado,
    NoEncontrado,
    LimiteSolicitudesExcedido,
    ServicioNoDisponible
}