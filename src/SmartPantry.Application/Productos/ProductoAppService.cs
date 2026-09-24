using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace SmartPantry.Productos;

public class ProductoAppService : ApplicationService, IProductoAppService
{
    private readonly IRepository<Producto, Guid> _productoRepository;

    public ProductoAppService(IRepository<Producto, Guid> productoRepository)
    {
        _productoRepository = productoRepository;
    }

    public async Task<ProductoDto> CreateAsync(CreateUpdateProductoDto input)
    {
        var producto = new Producto
        {
            CodigoBarras = input.CodigoBarras,
            Nombre = input.Nombre,
            Marca = input.Marca,
            Ingredientes = input.Ingredientes ?? new List<string>(),
            Alergenos = input.Alergenos ?? new List<string>()
        };

        await _productoRepository.InsertAsync(producto);

        return ObjectMapper.Map<Producto, ProductoDto>(producto);
    }

    public async Task<ProductoDto> GetAsync(Guid id)
    {
        var producto = await _productoRepository.GetAsync(id);
        return ObjectMapper.Map<Producto, ProductoDto>(producto);
    }
}