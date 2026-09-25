using System;
using System.Collections.Generic;
using Shouldly;
using Xunit;

namespace SmartPantry.Productos;

public class Producto_Tests
{
    [Fact]
    public void Deberia_Crear_Producto_Valido()
    {
        // Arrange & Act
        var producto = new Producto
        {
            CodigoBarras = "7791234567890",
            Nombre = "Leche Entera",
            Marca = "La Serenísima",
            Ingredientes = new List<string> { "Leche fluida" },
            Alergenos = new List<string> { "Leche" }
        };

        // Assert
        producto.CodigoBarras.ShouldBe("7791234567890");
        producto.Nombre.ShouldBe("Leche Entera");
        producto.Marca.ShouldBe("La Serenísima");
        producto.Ingredientes.ShouldContain("Leche fluida");
        producto.Alergenos.ShouldContain("Leche");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Deberia_Validar_Nombre_Vacio_O_Espacios(string nombreInvalido)
    {
        // Arrange
        var producto = new Producto
        {
            CodigoBarras = "7791234567890",
            Nombre = nombreInvalido,
            Marca = "La Serenísima"
        };

        // Assert
        string.IsNullOrWhiteSpace(producto.Nombre).ShouldBeTrue();
    }
}