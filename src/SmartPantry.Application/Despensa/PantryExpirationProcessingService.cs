using System;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Timing;
using Volo.Abp.Uow; 

namespace SmartPantry.Despensa;

public interface IPantryExpirationProcessingService
{
    Task ProcessExpirationsAsync();
}

public class PantryExpirationProcessingService : IPantryExpirationProcessingService, ITransientDependency
{
    private readonly IRepository<ItemDespensa, Guid> _itemDespensaRepository;
    private readonly IRepository<Advertencia, Guid> _advertenciaRepository;
    private readonly IClock _clock;

    public PantryExpirationProcessingService(
        IRepository<ItemDespensa, Guid> itemDespensaRepository,
        IRepository<Advertencia, Guid> advertenciaRepository,
        IClock clock)
    {
        _itemDespensaRepository = itemDespensaRepository;
        _advertenciaRepository = advertenciaRepository;
        _clock = clock;
    }

    [UnitOfWork] //  Esto evita el ObjectDisposedException gestionando la transacción y el ciclo de vida
    public async Task ProcessExpirationsAsync()
    {
        var today = _clock.Now.Date;
        int umbralDias = 3;
        var fechaUmbral = today.AddDays(umbralDias);

        var items = await _itemDespensaRepository.GetListAsync();

        foreach (var item in items)
        {
            if (item.Estado == "Consumido" || !item.FechaVencimiento.HasValue)
            {
                await DesactivarAdvertenciasSiExistenAsync(item.Id);
                continue;
            }

            var fechaVenc = item.FechaVencimiento.Value.Date;
            string? tipoAdvertencia = null;
            string? mensaje = null;

            if (fechaVenc < today)
            {
                tipoAdvertencia = "Vencido";
                mensaje = $"El ítem con ID {item.Id} se encuentra vencido.";
            }
            else if (fechaVenc <= fechaUmbral)
            {
                tipoAdvertencia = "ProximoAVencer";
                mensaje = $"El ítem con ID {item.Id} está próximo a vencer (Fecha: {fechaVenc:yyyy-MM-dd}).";
            }

            if (tipoAdvertencia != null && mensaje != null)
            {
                Advertencia? advertenciaExistente = await _advertenciaRepository.FirstOrDefaultAsync(
                    a => a.ItemDespensaId == item.Id && a.TipoAdvertencia == tipoAdvertencia
                );

                if (advertenciaExistente == null)
                {
                    var nuevaAdvertencia = new Advertencia
                    {
                        ItemDespensaId = item.Id,
                        TipoAdvertencia = tipoAdvertencia,
                        Mensaje = mensaje,
                        Activa = true
                    };
                    await _advertenciaRepository.InsertAsync(nuevaAdvertencia);
                }
                else
                {
                    advertenciaExistente.Activa = true;
                    advertenciaExistente.Mensaje = mensaje;
                    await _advertenciaRepository.UpdateAsync(advertenciaExistente);
                }
            }
            else
            {
                await DesactivarAdvertenciasSiExistenAsync(item.Id);
            }
        }
    }

    private async Task DesactivarAdvertenciasSiExistenAsync(Guid itemDespensaId)
    {
        var advertencias = await _advertenciaRepository.GetListAsync(a => a.ItemDespensaId == itemDespensaId && a.Activa);
        foreach (var adv in advertencias)
        {
            adv.Activa = false;
            await _advertenciaRepository.UpdateAsync(adv);
        }
    }
}