namespace OrderService.Services;

/// <summary>
/// Typed HttpClient that talks directly to ProductService on the internal
/// Docker network (http://productservice) — NOT via the Gateway.
///
/// Service-to-service calls on the internal network bypass the public Gateway;
/// the caller's JWT is forwarded so ProductService can still authorize the request.
/// </summary>
public class ProductServiceClient : IProductServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ProductServiceClient> _logger;

    public ProductServiceClient(HttpClient httpClient, ILogger<ProductServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger     = logger;
    }

    /// <inheritdoc/>
    public async Task<bool> ProductExistsAsync(int productId, string authorizationHeader)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/products/{productId}");

            // Forward the caller's JWT so ProductService can authenticate the request.
            // This is the standard pattern for service-to-service calls when each service
            // validates tokens independently (no shared internal-only bypass).
            if (!string.IsNullOrWhiteSpace(authorizationHeader))
            {
                request.Headers.TryAddWithoutValidation("Authorization", authorizationHeader);
            }

            var response = await _httpClient.SendAsync(request);

            _logger.LogInformation(
                "ProductService check for product {ProductId}: {StatusCode}",
                productId, response.StatusCode);

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to reach ProductService while checking product {ProductId}", productId);
            // Return false so the caller gets a 400 rather than an unhandled exception
            return false;
        }
    }
}
