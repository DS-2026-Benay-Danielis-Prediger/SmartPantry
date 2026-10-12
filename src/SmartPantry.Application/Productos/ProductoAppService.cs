using System;
using System.Threading.Tasks;
using SmartPantry.Productos;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace SmartPantry.Productos;

public class ProductoAppService :
    CrudAppService<
        Producto,                       // Entidad de Dominio[cite: 1]
        ProductoDto,                    // DTO devuelto
        Guid,                           // Clave primaria
        PagedAndSortedResultRequestDto, // Paginación del TP06[cite: 2]
        CreateUpdateProductoDto>,       // DTO para crear y modificar
    IProductoAppService
{
    public ProductoAppService(IRepository<Producto, Guid> repository)
        : base(repository)
    {
    }

    // Maneja la creación y reactivación de productos dados de baja
    public override async Task<ProductoDto> CreateAsync(CreateUpdateProductoDto input)
    {
        var productoExistente = await Repository.FirstOrDefaultAsync(p => p.CodigoBarras == input.CodigoBarras);

        if (productoExistente != null)
        {
            if (productoExistente.Activo)
            {
                throw new UserFriendlyException($"El producto con código de barras {input.CodigoBarras} ya existe y está activo.");
            }

            // Si estaba dado de baja, lo reactivamos y le actualizamos los datos
            productoExistente.Reactivar();
            productoExistente.ModificarDatos(
                input.Nombre,
                input.Marca,
                input.CodigoBarras,
                input.Ingredientes,
                input.Alergenos
            );

            await Repository.UpdateAsync(productoExistente);
            return ObjectMapper.Map<Producto, ProductoDto>(productoExistente);
        }

        return await base.CreateAsync(input);
    }

    // Sobrescribimos UpdateAsync para aplicar las validaciones del dominio
    public override async Task<ProductoDto> UpdateAsync(Guid id, CreateUpdateProductoDto input)
    {
        var producto = await Repository.GetAsync(id);

        producto.ModificarDatos(
            input.Nombre,
            input.Marca,
            input.CodigoBarras,
            input.Ingredientes,
            input.Alergenos
        );

        await Repository.UpdateAsync(producto);
        return ObjectMapper.Map<Producto, ProductoDto>(producto);
    }

    // Sobrescribimos DeleteAsync para hacer la BAJA LÓGICA (cambia Activo a false)
    public override async Task DeleteAsync(Guid id)
    {
        var producto = await Repository.GetAsync(id);

        producto.Desactivar(); // Llama al método de la entidad

        await Repository.UpdateAsync(producto);
    }
}