using System.Threading.Tasks;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using Xunit;

namespace SmartPantry.Productos;

public class ExternalProductAppService_Tests
{
    private readonly IExternalProductCatalogClient _mockCatalogClient;
    private readonly ExternalProductAppService _appService;

    public ExternalProductAppService_Tests()
    {
        _mockCatalogClient = Substitute.For<IExternalProductCatalogClient>();
        _appService = new ExternalProductAppService(_mockCatalogClient);
    }

    [Fact]
    public async Task Deberia_Retornar_Producto_Cuando_Existe()
    {
        var barcode = "3017620422003";
        _mockCatalogClient.GetByBarcodeAsync(barcode).Returns(new ExternalProductDto
        {
            CodigoBarras = barcode,
            Nombre = "Nutella",
            Marca = "Ferrero"
        });

        var result = await _appService.ConsultarPorCodigoAsync(new ConsultaProductoExternoDto { CodigoBarras = barcode });

        result.Estado.ShouldBe(EstadoConsultaProductoExterno.Encontrado);
        result.Nombre.ShouldBe("Nutella");
        result.Marca.ShouldBe("Ferrero");
    }

    [Fact]
    public async Task Deberia_Retornar_NoEncontrado_Cuando_No_Existe()
    {
        _mockCatalogClient.GetByBarcodeAsync("0000000000000").Returns((ExternalProductDto?)null);

        var result = await _appService.ConsultarPorCodigoAsync(new ConsultaProductoExternoDto { CodigoBarras = "0000000000000" });

        result.Estado.ShouldBe(EstadoConsultaProductoExterno.NoEncontrado);
    }

    [Fact]
    public async Task Deberia_Manejar_Limite_De_Solicitudes()
    {
        _mockCatalogClient.GetByBarcodeAsync(Arg.Any<string>()).Throws(new RateLimitExceededException());

        var result = await _appService.ConsultarPorCodigoAsync(new ConsultaProductoExternoDto { CodigoBarras = "3017620422003" });

        result.Estado.ShouldBe(EstadoConsultaProductoExterno.LimiteSolicitudesExcedido);
    }

    [Fact]
    public async Task Deberia_Manejar_Datos_Ausentes()
    {
        var barcode = "3017620422003";
        _mockCatalogClient.GetByBarcodeAsync(barcode).Returns(new ExternalProductDto
        {
            CodigoBarras = barcode,
            Nombre = null, // Simulamos datos ausentes
            Marca = null
        });

        var result = await _appService.ConsultarPorCodigoAsync(new ConsultaProductoExternoDto { CodigoBarras = barcode });

        result.Estado.ShouldBe(EstadoConsultaProductoExterno.Encontrado);
        result.Nombre.ShouldBeNullOrEmpty();
        result.Marca.ShouldBeNullOrEmpty();
    }
    [Fact]
    public async Task Deberia_Manejar_Servicio_No_Disponible()
    {
        _mockCatalogClient.GetByBarcodeAsync(Arg.Any<string>()).Throws(new ExternalServiceUnavailableException());

        var result = await _appService.ConsultarPorCodigoAsync(new ConsultaProductoExternoDto { CodigoBarras = "3017620422003" });

        result.Estado.ShouldBe(EstadoConsultaProductoExterno.ServicioNoDisponible);
    }
}