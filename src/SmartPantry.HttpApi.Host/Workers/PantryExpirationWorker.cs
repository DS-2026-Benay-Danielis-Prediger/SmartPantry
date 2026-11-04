using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;

public class PantryExpirationWorker : AsyncPeriodicBackgroundWorkerBase
{
    public PantryExpirationWorker(
        AbpAsyncTimer timer,
        IServiceScopeFactory serviceScopeFactory
    ) : base(timer, serviceScopeFactory)
    {
        // Configurar el intervalo de ejecución del temporizador (ej. cada 1 hora en milisegundos)
        Timer.Period = 3600000;
    }

    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        Logger.LogInformation("Iniciando verificación automática de vencimientos (RF-15)...");

        // Resolver el servicio de procesamiento dentro del scope actual
        var processingService = workerContext
            .ServiceProvider
            .GetRequiredService<PantryExpirationProcessingService>();

        await processingService.ProcessExpirationsAsync();

        Logger.LogInformation("Verificación automática de vencimientos finalizada.");
    }
}