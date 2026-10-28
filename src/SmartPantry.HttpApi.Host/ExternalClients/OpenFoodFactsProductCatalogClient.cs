using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using SmartPantry.Productos;

namespace SmartPantry.ExternalClients;

public class OpenFoodFactsProductCatalogClient : IExternalProductCatalogClient
{
    private readonly HttpClient _httpClient;

    public OpenFoodFactsProductCatalogClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ExternalProductDto?> GetByBarcodeAsync(string barcode)
    {
        const string fields = "code,product_name,product_name_es,brands,image_front_url,ingredients_text,allergens_tags";


        var url = $"product/{barcode}.json?fields={fields}";

        // Open Food Facts exige obligatoriamente un User-Agent personalizado para evitar bloqueos
        if (!_httpClient.DefaultRequestHeaders.Contains("User-Agent"))
        {
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "SmartPantry-UTN - Version 1.0");
        }

        var response = await _httpClient.GetAsync(url);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        if (response.StatusCode == HttpStatusCode.TooManyRequests)
        {
            throw new RateLimitExceededException();
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new ExternalServiceUnavailableException();
        }

        var jsonResult = await response.Content.ReadFromJsonAsync<OpenFoodFactsResponse>();

        if (jsonResult?.Product == null)
        {
            return null;
        }

        var p = jsonResult.Product;

        return new ExternalProductDto
        {
            CodigoBarras = barcode,
            Nombre = !string.IsNullOrWhiteSpace(p.ProductNameEs) ? p.ProductNameEs : p.ProductName,
            Marca = p.Brands,
            ImagenUrl = p.ImageFrontUrl,
            Ingredientes = string.IsNullOrWhiteSpace(p.IngredientsText) ? null : new List<string> { p.IngredientsText },
            Alergenos = p.AllergensTags
        };
    }

    private class OpenFoodFactsResponse
    {
        [JsonPropertyName("product")]
        public OpenFoodFactsProduct? Product { get; set; }
    }

    private class OpenFoodFactsProduct
    {
        [JsonPropertyName("product_name")]
        public string? ProductName { get; set; }

        [JsonPropertyName("product_name_es")]
        public string? ProductNameEs { get; set; }

        [JsonPropertyName("brands")]
        public string? Brands { get; set; }

        [JsonPropertyName("image_front_url")]
        public string? ImageFrontUrl { get; set; }

        [JsonPropertyName("ingredients_text")]
        public string? IngredientsText { get; set; }

        [JsonPropertyName("allergens_tags")]
        public List<string>? AllergensTags { get; set; }
    }
}