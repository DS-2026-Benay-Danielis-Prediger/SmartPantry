using System;
using System.Threading.Tasks;
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

    [Fact]
    public async Task Should_Process_Expirations_Correctly_All_Scenarios()
    {
        Guid itemVencidoId, itemProximoId, itemFueraId, itemSinFechaId, itemConsumidoId;

        //  Insertamos los casos de prueba con unidades de trabajo aisladas
        using (var uow = _unitOfWorkManager.Begin(requiresNew: true))
        {
            var today = _clock.Now.Date;

            var itemVencido = await _itemRepository.InsertAsync(new ItemDespensa { Estado = "Activo", FechaVencimiento = today.AddDays(-2) });
            var itemProximo = await _itemRepository.InsertAsync(new ItemDespensa { Estado = "Activo", FechaVencimiento = today.AddDays(2) });
            var itemFuera = await _itemRepository.InsertAsync(new ItemDespensa { Estado = "Activo", FechaVencimiento = today.AddDays(10) });
            var itemSinFecha = await _itemRepository.InsertAsync(new ItemDespensa { Estado = "Activo", FechaVencimiento = null });
            var itemConsumido = await _itemRepository.InsertAsync(new ItemDespensa { Estado = "Consumido", FechaVencimiento = today.AddDays(-5) });

            await uow.CompleteAsync();

            itemVencidoId = itemVencido.Id;
            itemProximoId = itemProximo.Id;
            itemFueraId = itemFuera.Id;
            itemSinFechaId = itemSinFecha.Id;
            itemConsumidoId = itemConsumido.Id;
        }

        //  Ejecutamos el servicio de procesamiento
        await _processingService.ProcessExpirationsAsync();

        //  Validamos que se hayan generado las advertencias correctas
        using (var uow = _unitOfWorkManager.Begin(requiresNew: true))
        {
            var advVencido = await _advertenciaRepository.FirstOrDefaultAsync(a => a.ItemDespensaId == itemVencidoId);
            advVencido.ShouldNotBeNull();
            advVencido.TipoAdvertencia.ShouldBe("Vencido");
            advVencido.Activa.ShouldBeTrue();

            var advProximo = await _advertenciaRepository.FirstOrDefaultAsync(a => a.ItemDespensaId == itemProximoId);
            advProximo.ShouldNotBeNull();
            advProximo.TipoAdvertencia.ShouldBe("ProximoAVencer");
            advProximo.Activa.ShouldBeTrue();

            var advFuera = await _advertenciaRepository.FirstOrDefaultAsync(a => a.ItemDespensaId == itemFueraId);
            if (advFuera != null) advFuera.Activa.ShouldBeFalse();

            var advSinFecha = await _advertenciaRepository.FirstOrDefaultAsync(a => a.ItemDespensaId == itemSinFechaId);
            if (advSinFecha != null) advSinFecha.Activa.ShouldBeFalse();

            var advConsumido = await _advertenciaRepository.FirstOrDefaultAsync(a => a.ItemDespensaId == itemConsumidoId);
            if (advConsumido != null) advConsumido.Activa.ShouldBeFalse();

            await uow.CompleteAsync();
        }

        //  Idempotencia (ejecutar de nuevo no debe duplicar advertencias)
        await _processingService.ProcessExpirationsAsync();

        using (var uow = _unitOfWorkManager.Begin(requiresNew: true))
        {
            int totalAdvertencias = (await _advertenciaRepository.GetListAsync()).Count;
            totalAdvertencias.ShouldBe(2); // Solo vencido y próximo
            await uow.CompleteAsync();
        }

        //  Cambio de estado dinámico (el ítem próximo ahora pasa a Consumido)
        using (var uow = _unitOfWorkManager.Begin(requiresNew: true))
        {
            var itemProximo = await _itemRepository.GetAsync(itemProximoId);
            itemProximo.Estado = "Consumido";
            await _itemRepository.UpdateAsync(itemProximo);
            await uow.CompleteAsync();
        }

        await _processingService.ProcessExpirationsAsync();

        // Validar desactivación automática
        using (var uow = _unitOfWorkManager.Begin(requiresNew: true))
        {
            var advProximoActualizada = await _advertenciaRepository.GetAsync(a => a.ItemDespensaId == itemProximoId);
            advProximoActualizada.Activa.ShouldBeFalse();
            await uow.CompleteAsync();
        }
    }
}