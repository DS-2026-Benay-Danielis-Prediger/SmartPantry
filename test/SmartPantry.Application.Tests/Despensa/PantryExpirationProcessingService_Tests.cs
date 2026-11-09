using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Timing;
using Volo.Abp.Uow;
using Xunit;

namespace SmartPantry.Despensa;

public class PantryExpirationProcessingService_Tests : SmartPantryApplicationTestBase<SmartPantryApplicationTestModule>
{
    private readonly IPantryExpirationProcessingService _processingService;
    private readonly IRepository<ItemDespensa, Guid> _itemRepository;
    private readonly IRepository<Advertencia, Guid> _advertenciaRepository;
    private readonly IClock _clock;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public PantryExpirationProcessingService_Tests()
    {
        _processingService = GetRequiredService<IPantryExpirationProcessingService>();
        _itemRepository = GetRequiredService<IRepository<ItemDespensa, Guid>>();
        _advertenciaRepository = GetRequiredService<IRepository<Advertencia, Guid>>();
        _clock = GetRequiredService<IClock>();
        _unitOfWorkManager = GetRequiredService<IUnitOfWorkManager>();
    }

    private async Task ProcesarEnScopeAsync()
    {
        using var scope = ServiceProvider.CreateScope();
        var svc = scope.ServiceProvider.GetRequiredService<IPantryExpirationProcessingService>();
        await svc.ProcessExpirationsAsync();
    }

    [Fact]
    public async Task Debe_Generar_Advertencia_Cuando_Vence_En_Exactamente_3_Dias()
    {
        Guid itemLimiteId;

        using (var uow = _unitOfWorkManager.Begin(requiresNew: true))
        {
            var today = _clock.Now.Date;
            var item = await _itemRepository.InsertAsync(new ItemDespensa { Estado = "Activo", FechaVencimiento = today.AddDays(3) });
            await uow.CompleteAsync();
            itemLimiteId = item.Id;
        }

        await _processingService.ProcessExpirationsAsync();

        using (var uow = _unitOfWorkManager.Begin(requiresNew: true))
        {
            var adv = await _advertenciaRepository.FirstOrDefaultAsync(a => a.ItemDespensaId == itemLimiteId);
            adv.ShouldNotBeNull();
            adv.Activa.ShouldBeTrue();
            adv.TipoAdvertencia.ShouldBe("ProximoAVencer");
            await uow.CompleteAsync();
        }
    }

    [Fact]
    public async Task No_Debe_Duplicar_Advertencia_En_Ejecuciones_Concurrentes()
    {
        Guid itemId;

        using (var uow = _unitOfWorkManager.Begin(requiresNew: true))
        {
            var item = await _itemRepository.InsertAsync(new ItemDespensa { Estado = "Activo", FechaVencimiento = _clock.Now.Date.AddDays(3) });
            await uow.CompleteAsync();
            itemId = item.Id;
        }

        // Sin procesamiento previo: múltiples ejecuciones paralelas intentan crear la advertencia al mismo tiempo
        await Task.WhenAll(
            ProcesarEnScopeAsync(),
            ProcesarEnScopeAsync(),
            ProcesarEnScopeAsync()
        );

        using (var uow = _unitOfWorkManager.Begin(requiresNew: true))
        {
            var list = await _advertenciaRepository.GetListAsync(a => a.ItemDespensaId == itemId);
            list.Count.ShouldBe(1);
            await uow.CompleteAsync();
        }
    }

    [Fact]
    public async Task Debe_Actualizar_Advertencia_Existente_Cuando_Cambia_La_Fecha()
    {
        Guid itemId;
        Guid initialAdvId;
        string initialMessage;

        // 1. Partir de una advertencia existente (item en +3 días)
        using (var uow = _unitOfWorkManager.Begin(requiresNew: true))
        {
            var today = _clock.Now.Date;
            var item = await _itemRepository.InsertAsync(new ItemDespensa { Estado = "Activo", FechaVencimiento = today.AddDays(3) });
            await uow.CompleteAsync();
            itemId = item.Id;
        }

        await _processingService.ProcessExpirationsAsync();

        using (var uow = _unitOfWorkManager.Begin(requiresNew: true))
        {
            var adv = await _advertenciaRepository.FirstOrDefaultAsync(a => a.ItemDespensaId == itemId);
            adv.ShouldNotBeNull();
            initialAdvId = adv.Id;
            initialMessage = adv.Mensaje;
            await uow.CompleteAsync();
        }

        // 2. Cambiar la fecha (a +1 día) para forzar actualización sobre la misma fila
        using (var uow = _unitOfWorkManager.Begin(requiresNew: true))
        {
            var item = await _itemRepository.GetAsync(itemId);
            item.FechaVencimiento = _clock.Now.Date.AddDays(1);
            await _itemRepository.UpdateAsync(item);
            await uow.CompleteAsync();
        }

        await _processingService.ProcessExpirationsAsync();

        // 3. Verificar que se actualizó la misma fila, sin duplicar y que el mensaje cambió
        using (var uow = _unitOfWorkManager.Begin(requiresNew: true))
        {
            var totalAdvs = await _advertenciaRepository.GetListAsync(a => a.ItemDespensaId == itemId);
            totalAdvs.Count.ShouldBe(1);
            totalAdvs[0].Id.ShouldBe(initialAdvId); // Mismo ID de advertencia (misma fila)
            totalAdvs[0].Mensaje.ShouldNotBe(initialMessage); // El mensaje cambió al cambiar la fecha
            await uow.CompleteAsync();
        }
    }

    [Fact]
    public async Task Debe_Desactivar_Y_Reactivar_Advertencia_Cuando_Cambia_La_Fecha()
    {
        Guid itemId;
        Guid advId;

        // 1. Advertencia activa inicial (fecha a +2)
        using (var uow = _unitOfWorkManager.Begin(requiresNew: true))
        {
            var today = _clock.Now.Date;
            var item = await _itemRepository.InsertAsync(new ItemDespensa { Estado = "Activo", FechaVencimiento = today.AddDays(2) });
            await uow.CompleteAsync();
            itemId = item.Id;
        }

        await _processingService.ProcessExpirationsAsync();

        using (var uow = _unitOfWorkManager.Begin(requiresNew: true))
        {
            var adv = await _advertenciaRepository.FirstOrDefaultAsync(a => a.ItemDespensaId == itemId);
            adv.ShouldNotBeNull();
            adv.Activa.ShouldBeTrue();
            advId = adv.Id;
            await uow.CompleteAsync();
        }

        // 2. Cambiar fecha a +10 (fuera de umbral) -> Activa == false
        using (var uow = _unitOfWorkManager.Begin(requiresNew: true))
        {
            var item = await _itemRepository.GetAsync(itemId);
            item.FechaVencimiento = _clock.Now.Date.AddDays(10);
            await _itemRepository.UpdateAsync(item);
            await uow.CompleteAsync();
        }

        await _processingService.ProcessExpirationsAsync();

        using (var uow = _unitOfWorkManager.Begin(requiresNew: true))
        {
            var adv = await _advertenciaRepository.GetAsync(advId);
            adv.Activa.ShouldBeFalse(); // Debe haberse desactivado
            await uow.CompleteAsync();
        }

        // 3. Volver a cambiar fecha a +2 -> Activa == true en la misma fila (Reactivación)
        using (var uow = _unitOfWorkManager.Begin(requiresNew: true))
        {
            var item = await _itemRepository.GetAsync(itemId);
            item.FechaVencimiento = _clock.Now.Date.AddDays(2);
            await _itemRepository.UpdateAsync(item);
            await uow.CompleteAsync();
        }

        await _processingService.ProcessExpirationsAsync();

        using (var uow = _unitOfWorkManager.Begin(requiresNew: true))
        {
            var totalAdvs = await _advertenciaRepository.GetListAsync(a => a.ItemDespensaId == itemId);
            totalAdvs.Count.ShouldBe(1);
            totalAdvs[0].Id.ShouldBe(advId); // Misma fila
            totalAdvs[0].Activa.ShouldBeTrue(); // Reactivada
            await uow.CompleteAsync();
        }
    }

    [Fact]
    public async Task Debe_Desactivar_Advertencia_Cuando_Se_Consume_El_Item()
    {
        Guid itemId;

        using (var uow = _unitOfWorkManager.Begin(requiresNew: true))
        {
            var item = await _itemRepository.InsertAsync(new ItemDespensa { Estado = "Activo", FechaVencimiento = _clock.Now.Date.AddDays(2) });
            await uow.CompleteAsync();
            itemId = item.Id;
        }

        await _processingService.ProcessExpirationsAsync();

        using (var uow = _unitOfWorkManager.Begin(requiresNew: true))
        {
            var adv = await _advertenciaRepository.FirstOrDefaultAsync(a => a.ItemDespensaId == itemId);
            adv.ShouldNotBeNull();
            adv.Activa.ShouldBeTrue();
            await uow.CompleteAsync();
        }

        using (var uow = _unitOfWorkManager.Begin(requiresNew: true))
        {
            var item = await _itemRepository.GetAsync(itemId);
            item.Estado = "Consumido";
            await _itemRepository.UpdateAsync(item);
            await uow.CompleteAsync();
        }

        await _processingService.ProcessExpirationsAsync();

        using (var uow = _unitOfWorkManager.Begin(requiresNew: true))
        {
            var adv = await _advertenciaRepository.FirstAsync(a => a.ItemDespensaId == itemId);
            adv.Activa.ShouldBeFalse();
            await uow.CompleteAsync();
        }
    }

    [Fact]
    public async Task Debe_Procesar_Vencidos_Sin_Fecha_Consumidos_Y_Fuera_De_Umbral_Sin_Repetir()
    {
        Guid itemVencidoId, item4Id, item10Id, itemSinFechaId, itemConsumidoId;

        using (var uow = _unitOfWorkManager.Begin(requiresNew: true))
        {
            var today = _clock.Now.Date;
            itemVencidoId = (await _itemRepository.InsertAsync(new ItemDespensa { Estado = "Activo", FechaVencimiento = today.AddDays(-2) })).Id;
            item4Id = (await _itemRepository.InsertAsync(new ItemDespensa { Estado = "Activo", FechaVencimiento = today.AddDays(4) })).Id;
            item10Id = (await _itemRepository.InsertAsync(new ItemDespensa { Estado = "Activo", FechaVencimiento = today.AddDays(10) })).Id;
            itemSinFechaId = (await _itemRepository.InsertAsync(new ItemDespensa { Estado = "Activo", FechaVencimiento = null })).Id;
            itemConsumidoId = (await _itemRepository.InsertAsync(new ItemDespensa { Estado = "Consumido", FechaVencimiento = today.AddDays(-1) })).Id;
            await uow.CompleteAsync();
        }

        await _processingService.ProcessExpirationsAsync();

        using (var uow = _unitOfWorkManager.Begin(requiresNew: true))
        {
            // El vencido sí genera "Vencido"
            var advVencido = await _advertenciaRepository.FirstOrDefaultAsync(a => a.ItemDespensaId == itemVencidoId);
            advVencido.ShouldNotBeNull();
            advVencido.TipoAdvertencia.ShouldBe("Vencido");

            // Los demás no generan nada
            foreach (var id in new[] { item4Id, item10Id, itemSinFechaId, itemConsumidoId })
            {
                (await _advertenciaRepository.GetListAsync(a => a.ItemDespensaId == id)).ShouldBeEmpty();
            }

            await uow.CompleteAsync();
        }

        // Segunda ejecución: no debe cambiar nada (idempotencia / repetición secuencial)
        int countBefore;
        using (var uow = _unitOfWorkManager.Begin(requiresNew: true))
        {
            countBefore = (await _advertenciaRepository.GetListAsync()).Count;
            await uow.CompleteAsync();
        }

        await _processingService.ProcessExpirationsAsync();

        using (var uow = _unitOfWorkManager.Begin(requiresNew: true))
        {
            (await _advertenciaRepository.GetListAsync()).Count.ShouldBe(countBefore);
            await uow.CompleteAsync();
        }
    }
}