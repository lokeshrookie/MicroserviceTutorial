using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductService.Data;
using ProductService.Models;
using System.ComponentModel.DataAnnotations;

namespace ProductService.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]  // All endpoints require a valid JWT
public class ProductsController : ControllerBase
{
    private readonly ProductDbContext _db;

    public ProductsController(ProductDbContext db)
    {
        _db = db;
    }

    // ─── GET /api/products ────────────────────────────────────────────────────

    /// <summary>Returns all products.</summary>
    [HttpGet]
    [Authorize(Roles = "Admin,User")]
    public async Task<ActionResult<IEnumerable<Product>>> GetProducts()
    {
        var products = await _db.Products.ToListAsync();
        return Ok(products);
    }

    // ─── GET /api/products/{id} ───────────────────────────────────────────────

    /// <summary>Returns a single product by ID.</summary>
    [HttpGet("{id:int}")]
    [Authorize(Roles = "Admin,User")]
    public async Task<ActionResult<Product>> GetProduct(int id)
    {
        var product = await _db.Products.FindAsync(id);
        if (product is null)
            return NotFound(new { Message = $"Product with ID {id} was not found." });

        return Ok(product);
    }

    // ─── POST /api/products ───────────────────────────────────────────────────

    /// <summary>Creates a new product. Admin role required.</summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<Product>> CreateProduct([FromBody] CreateProductRequest request)
    {
        var product = new Product
        {
            Name  = request.Name.Trim(),
            Price = request.Price
        };

        _db.Products.Add(product);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
    }

    // ─── PUT /api/products/{id} ───────────────────────────────────────────────

    /// <summary>Updates an existing product. Admin role required.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateProduct(int id, [FromBody] UpdateProductRequest request)
    {
        var product = await _db.Products.FindAsync(id);
        if (product is null)
            return NotFound(new { Message = $"Product with ID {id} was not found." });

        product.Name  = request.Name.Trim();
        product.Price = request.Price;

        await _db.SaveChangesAsync();

        return Ok(product);
    }

    // ─── DELETE /api/products/{id} ────────────────────────────────────────────

    /// <summary>Deletes a product by ID. Admin role required.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        var product = await _db.Products.FindAsync(id);
        if (product is null)
            return NotFound(new { Message = $"Product with ID {id} was not found." });

        _db.Products.Remove(product);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}

// ─── Request DTOs ─────────────────────────────────────────────────────────────

/// <summary>Payload for creating a product.</summary>
public record CreateProductRequest(
    [Required][StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 100 characters.")] string Name,
    [Range(0.01, 1_000_000, ErrorMessage = "Price must be between 0.01 and 1,000,000.")] decimal Price
);

/// <summary>Payload for updating a product.</summary>
public record UpdateProductRequest(
    [Required][StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 100 characters.")] string Name,
    [Range(0.01, 1_000_000, ErrorMessage = "Price must be between 0.01 and 1,000,000.")] decimal Price
);
