namespace OrderService.Services;

/// <summary>
/// Abstraction for communicating with the ProductService.
/// Lets OrderService verify a product exists before creating an order.
/// </summary>
public interface IProductServiceClient
{
    /// <summary>
    /// Returns true if a product with the given ID exists in ProductService.
    /// The caller's Authorization header is forwarded so ProductService can
    /// validate the JWT (since its endpoints require authentication).
    /// </summary>
    Task<bool> ProductExistsAsync(int productId, string authorizationHeader);
}
