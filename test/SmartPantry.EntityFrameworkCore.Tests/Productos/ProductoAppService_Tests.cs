using Shouldly;
using SmartPantry.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace SmartPantry.Productos;

public class ProductoAppService_Tests : SmartPantryEntityFrameworkCoreTestBase
{
    private readonly IProductoAppService _productoAppService;

    public ProductoAppService_Tests()
    {
        _productoAppService = GetRequiredService<IProductoAppService>();
    }

    [Fact]
    public async Task Deberia_Crear_Y_Obtener_Producto()
    {
        // Arrange
        var createDto = new CreateUpdateProductoDto
        {
            CodigoBarras = "7791234567890",
            Nombre = "Leche Entera",
            Marca = "La Serenísima",
            Ingredientes = new List<string> { "Leche fluida" },
            Alergenos = new List<string> { "Leche" }
        };

        // Act - Crear
        var productoCreado = await _productoAppService.CreateAsync(createDto);

        // Assert - Crear
        productoCreado.Id.ShouldNotBe(default);
        productoCreado.Nombre.ShouldBe("Leche Entera");

        // Act - Obtener por ID
        var productoObtenido = await _productoAppService.GetAsync(productoCreado.Id);

        // Assert - Obtener
        productoObtenido.ShouldNotBeNull();
        productoObtenido.Id.ShouldBe(productoCreado.Id);
        productoObtenido.Nombre.ShouldBe("Leche Entera");
    }

[Fact]
    public async Task Deberia_Rechazar_Creacion_Si_Dto_No_Tiene_Nombre()
    {
        // Arrange - DTO con Nombre vacío (dato obligatorio)
        var createDto = new CreateUpdateProductoDto
        {
            CodigoBarras = "7791234567890",
            Nombre = "", // Inválido
            Marca = "La Serenísima"
        };

        // Act & Assert - ABP debe lanzar una excepción de validación (AbpValidationException)
        await Should.ThrowAsync<Volo.Abp.Validation.AbpValidationException>(async () =>
        {
            await _productoAppService.CreateAsync(createDto);
        });
    }
}