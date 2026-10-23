using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace SmartPantry.Productos;

public class ExternalProductAppService : ApplicationService, IExternalProductAppService
{
    private readonly IExternalProductCatalogClient _externalCatalogClient;

    public ExternalProductAppService(IExternalProductCatalogClient externalCatalogClient)
    {
        _externalCatalogClient = externalCatalogClient;
    }

    public async Task<ProductoExternoResultadoDto> ConsultarPorCodigoAsync(ConsultaProductoExternoDto input)
    {
        try
        {
            var result = await _externalCatalogClient.GetByBarcodeAsync(input.CodigoBarras);

            if (result == null)
            {
                return new ProductoExternoResultadoDto
                {
                    Estado = EstadoConsultaProductoExterno.NoEncontrado,
                    MensajeError = "El producto no existe en Open Food Facts."
                };
            }

            return new ProductoExternoResultadoDto
            {
                Estado = EstadoConsultaProductoExterno.Encontrado,
                CodigoBarras = result.CodigoBarras,
                Nombre = result.Nombre,
                Marca = result.Marca,
                ImagenUrl = result.ImagenUrl,
                Ingredientes = result.Ingredientes,
                Alergenos = result.Alergenos
            };
        }
        catch (RateLimitExceededException)
        {
            return new ProductoExternoResultadoDto
            {
                Estado = EstadoConsultaProductoExterno.LimiteSolicitudesExcedido,
                MensajeError = "Se ha superado el límite de solicitudes al proveedor externo."
            };
        }
        catch (ExternalServiceUnavailableException)
        {
            return new ProductoExternoResultadoDto
            {
                Estado = EstadoConsultaProductoExterno.ServicioNoDisponible,
                MensajeError = "El servicio de Open Food Facts no está disponible temporalmente."
            };
        }
    }
}

// Excepciones de control
public class RateLimitExceededException : Exception { }
public class ExternalServiceUnavailableException : Exception { }