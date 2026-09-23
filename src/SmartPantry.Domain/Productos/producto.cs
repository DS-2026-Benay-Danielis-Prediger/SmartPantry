using System;
using System.Collections.Generic;
using Volo.Abp.Domain.Entities;

namespace SmartPantry.Productos;

public class Producto : BasicAggregateRoot<Guid>
{
    public string CodigoBarras { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Marca { get; set; } = string.Empty;
    public List<string> Ingredientes { get; set; } = new();
    public List<string> Alergenos { get; set; } = new();
}