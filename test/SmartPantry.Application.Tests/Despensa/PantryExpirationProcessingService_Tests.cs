using System;
using System.Threading.Tasks;
using Shouldly;
using Xunit;

namespace SmartPantry.Despensa;

public class PantryExpirationProcessingService_Tests : SmartPantryTestBase<SmartPantryApplicationTestModule>
{
    private readonly IPantryExpirationProcessingService _processingService;

    public PantryExpirationProcessingService_Tests()
    {
        _processingService = GetRequiredService<IPantryExpirationProcessingService>();
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