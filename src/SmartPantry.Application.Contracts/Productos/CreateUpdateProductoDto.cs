using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SmartPantry.Productos;

public class CreateUpdateProductoDto
{
    [Required]
    [StringLength(64)]
    public string CodigoBarras { get; set; } = string.Empty;

    [Required]
    [StringLength(128)]
    public string Nombre { get; set; } = string.Empty;

    [Required]
    [StringLength(128)]
    public string Marca { get; set; } = string.Empty;

    public List<string> Ingredientes { get; set; } = new();
    public List<string> Alergenos { get; set; } = new();
}
