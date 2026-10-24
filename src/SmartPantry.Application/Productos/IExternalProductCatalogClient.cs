using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmartPantry.Productos;

public class ExternalProductDto
{
    public string CodigoBarras { get; set; } = string.Empty;
    public string? Nombre { get; set; }
    public string? Marca { get; set; }
    public string? ImagenUrl { get; set; }
    public List<string>? Ingredientes { get; set; }
    public List<string>? Alergenos { get; set; }
}

public interface IExternalProductCatalogClient
{
    Task<ExternalProductDto?> GetByBarcodeAsync(string barcode);
}