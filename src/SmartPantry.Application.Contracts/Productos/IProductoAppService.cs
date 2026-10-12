using System;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace SmartPantry.Productos;

public interface IProductoAppService :
    ICrudAppService<
        ProductoDto,                    // DTO que devuelve
        Guid,                           // ID de la entidad
        PagedAndSortedResultRequestDto, // Paginación y orden del TP06[cite: 2]
        CreateUpdateProductoDto>        // DTO para crear y modificar
{
}