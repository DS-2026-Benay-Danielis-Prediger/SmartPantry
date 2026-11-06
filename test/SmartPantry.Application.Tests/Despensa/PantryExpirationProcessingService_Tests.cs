using System;
using System.Threading.Tasks;
using Shouldly;
using Volo.Abp.Timing;
using Xunit;

namespace SmartPantry.Despensa;

public class PantryExpirationProcessingService_Tests : SmartPantryTestBase<SmartPantryApplicationTestModule>
{
    private readonly IPantryExpirationProcessingService _processingService;
    private readonly IClock _clock;

    public PantryExpirationProcessingService_Tests()
    {
        _processingService = GetRequiredService<IPantryExpirationProcessingService>();
        _clock = GetRequiredService<IClock>();
    }

    [Fact]
    public async Task Should_Process_Expirations_Correctly()
    {
        await Should.NotThrowAsync(async () =>
        {
            await _processingService.ProcessExpirationsAsync();
        });
    }
}