using System;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Uow;

namespace SmartPantry.Despensa;

public class PantryExpirationProcessingService : ITransientDependency
{
    private readonly IRepository<ItemDespensa, Guid> _itemDespensaRepository;
    private readonly IRepository<Advertencia, Guid> _advertenciaRepository;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public PantryExpirationProcessingService(
        IRepository<ItemDespensa, Guid> itemDespensaRepository,
        IRepository<Advertencia, Guid> advertenciaRepository,
        IUnitOfWorkManager unitOfWorkManager)
    {
        _itemDespensaRepository = itemDespensaRepository;
        _advertenciaRepository = advertenciaRepository;
        _unitOfWorkManager = unitOfWorkManager;
    }

    public async Task ProcessExpirationsAsync()
    {
        using var uow = _unitOfWorkManager.Begin(requiresNew: true, isTransactional: true);

        // Criterio unificado en UTC (exigencia del TP)
        var today = DateTime.UtcNow.Date;
        int umbralDias = 3;
        var fechaUmbral = today.AddDays(umbralDias);

        // Obtener todos los ítems de despensa
        var items = await _itemDespensaRepository.GetListAsync();

        foreach (var item in items)
        {
            // 1. Omitir si ya está consumido o no tiene fecha de vencimiento
            if (item.Estado == "Consumido" || !item.FechaVencimiento.HasValue)
            {
                // Si tuviera una advertencia activa previa, la desactivamos
                await DesactivarAdvertenciasSiExistenAsync(item.Id);
                continue;
            }

            var fechaVenc = item.FechaVencimiento.Value.Date;
            string tipoAdvertencia = null;
            string mensaje = null;

            // 2. Evaluar reglas de negocio (Vencido vs Próximo a vencer)
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

            // 3. Gestionar la advertencia aplicando idempotencia
            if (tipoAdvertencia != null)
            {
                var advertenciaExistente = await _advertenciaRepository.FirstOrDefaultAsync(
                    a => a.ItemDespensaId == item.Id && a.TipoAdvertencia == tipoAdvertencia
                );

                if (advertenciaExistente == null)
                {
                    // Crear nueva advertencia (protegida por el índice único de la BD)
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
                    // Si ya existe, nos aseguramos de que esté activa y actualizamos el mensaje si cambió
                    advertenciaExistente.Activa = true;
                    advertenciaExistente.Mensaje = mensaje;
                    await _advertenciaRepository.UpdateAsync(advertenciaExistente);
                }
            }
            else
            {
                // Si el ítem ya está fuera del umbral de riesgo, desactivamos sus advertencias
                await DesactivarAdvertenciasSiExistenAsync(item.Id);
            }
        }

        await uow.CompleteAsync();
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