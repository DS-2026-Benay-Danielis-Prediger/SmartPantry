using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;
using SmartPantry.Despensa;

namespace SmartPantry;

public class PantryExpirationWorker : AsyncPeriodicBackgroundWorkerBase
{
    public PantryExpirationWorker(
        AbpAsyncTimer timer,
        IServiceScopeFactory serviceScopeFactory
    ) : base(timer, serviceScopeFactory)
    {
        // Intervalo de ejecución configurado a 10 segundos para pruebas
        Timer.Period = 10000;
    }

    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        Logger.LogInformation("Iniciando verificación automática de vencimientos (RF-15)...");

        // Se resuelve utilizando la interfaz correcta
        var processingService = workerContext
            .ServiceProvider
            .GetRequiredService<PantryExpirationProcessingService>();

        await processingService.ProcessExpirationsAsync();
    }
}