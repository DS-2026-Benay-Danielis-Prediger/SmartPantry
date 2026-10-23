using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace SmartPantry.Productos;

public interface IExternalProductAppService : IApplicationService
{
    Task<ProductoExternoResultadoDto> ConsultarPorCodigoAsync(ConsultaProductoExternoDto input);
}