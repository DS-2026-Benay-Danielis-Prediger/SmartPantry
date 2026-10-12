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

    // Estado para la baja lógica[cite: 1]
    public bool Activo { get; set; } = true;

    // Métodos para cambiar de estado según reglas de negocio
    public void ModificarDatos(string nombre, string marca, string codigoBarras, List<string> ingredientes, List<string> alergenos)
    {
        Nombre = nombre;
        Marca = marca;
        CodigoBarras = codigoBarras;
        Ingredientes = ingredientes ?? new();
        Alergenos = alergenos ?? new();
    }

    public void Desactivar()
    {
        Activo = false;
    }

    public void Reactivar()
    {
        Activo = true;
    }
}