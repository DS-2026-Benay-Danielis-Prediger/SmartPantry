using System.ComponentModel.DataAnnotations;

namespace SmartPantry.Productos;

public class ConsultaProductoExternoDto
{
    [Required]
    [StringLength(14, MinimumLength = 8, ErrorMessage = "El código de barras debe tener entre 8 y 14 dígitos.")]
    public string CodigoBarras { get; set; } = string.Empty;
}