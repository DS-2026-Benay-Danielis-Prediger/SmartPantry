using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SmartPantry.Productos;

public class CreateUpdateProductoDto
{
    [Required]
    public string CodigoBarras { get; set; } = string.Empty;

    [Required]
    public string Nombre { get; set; } = string.Empty;

    [Required]
    public string Marca { get; set; } = string.Empty;

    public List<string> Ingredientes { get; set; } = new();
    public List<string> Alergenos { get; set; } = new();
}