using Microsoft.AspNetCore.Mvc;
using GoNomads.Shared.Models;
using Dapr.Client;

namespace ProductService.Controllers;

/// <summary>
/// Products API - RESTful endpoints for product management
/// </summary>
[ApiController]
[Route("api/v1/products")]
public class ProductsController : ControllerBase
{
    private static readonly List<Product> Products = new();
    private readonly ILogger<ProductsController> _logger;
    private readonly DaprClient _daprClient;

    public ProductsController(ILogger<ProductsController> logger, DaprClient daprClient)
    {
        _logger = logger;
        _daprClient = daprClient;
        
        // Initialize with sample data
        if (!Products.Any())
        {
            Products.AddRange(new[]
            {
                new Product 
                { 
                    Id = "1", 
                    Name = "Laptop", 
                    Description = "High-performance laptop", 
                    Price = 999.99m, 
                    UserId = "1", 
                    Category = "Electronics" 
                },
                new Product 
                { 
                    Id = "2", 
                    Name = "Coffee Mug", 
                    Description = "Ceramic coffee mug", 
                    Price = 15.99m, 
                    UserId = "2", 
                    Category = "Home & Kitchen" 
                }
            });
        }
    }

    [HttpGet]
    public ActionResult<ApiResponse<PaginatedResponse<Product>>> GetProducts([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        _logger.LogInformation("Getting products - Page: {Page}, PageSize: {PageSize}", page, pageSize);
        
        page = Math.Max(1, page);
        pageSize = Math.Max(1, Math.Min(100, pageSize));
        
        var skip = (page - 1) * pageSize;
        var pagedProducts = Products.Skip(skip).Take(pageSize).ToList();

        var response = new ApiResponse<PaginatedResponse<Product>>
        {
            Success = true,
            Message = "Products retrieved successfully",
            Data = new PaginatedResponse<Product>
            {
                Items = pagedProducts,
                TotalCount = Products.Count,
                Page = page,
                PageSize = pageSize
            }
        };

        return Ok(response);
    }

    [HttpGet("{id}")]
    public ActionResult<ApiResponse<Product>> GetProduct(string id)
    {
        _logger.LogInformation("Getting product with ID: {ProductId}", id);
        
        var product = Products.FirstOrDefault(p => p.Id == id);
        
        if (product == null)
        {
            return NotFound(new ApiResponse<Product>
            {
                Success = false,
                Message = "Product not found"
            });
        }

        return Ok(new ApiResponse<Product>
        {
            Success = true,
            Message = "Product retrieved successfully",
            Data = product
        });
    }

    [HttpGet("user/{userId}")]
    public ActionResult<ApiResponse<PaginatedResponse<Product>>> GetProductsByUserId(
        string userId, 
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 10)
    {
        _logger.LogInformation("Getting products for user: {UserId} - Page: {Page}, PageSize: {PageSize}", 
            userId, page, pageSize);
        
        page = Math.Max(1, page);
        pageSize = Math.Max(1, Math.Min(100, pageSize));
        
        var userProducts = Products.Where(p => p.UserId == userId).ToList();
        var skip = (page - 1) * pageSize;
        var pagedProducts = userProducts.Skip(skip).Take(pageSize).ToList();

        var response = new ApiResponse<PaginatedResponse<Product>>
        {
            Success = true,
            Message = "Products retrieved successfully",
            Data = new PaginatedResponse<Product>
            {
                Items = pagedProducts,
                TotalCount = userProducts.Count,
                Page = page,
                PageSize = pageSize
            }
        };

        return Ok(response);
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<Product>>> CreateProduct([FromBody] CreateProductDto dto)
    {
        _logger.LogInformation("Creating product: {ProductName} for user: {UserId}", dto.Name, dto.UserId);
        
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return BadRequest(new ApiResponse<Product>
            {
                Success = false,
                Message = "Validation failed",
                Errors = errors
            });
        }

        // Validate user exists via Dapr service invocation
        try
        {
            var userResponse = await _daprClient.InvokeMethodAsync<ApiResponse<object>>(
                "user-service", 
                $"api/users/{dto.UserId}");
            
            if (userResponse?.Success != true)
            {
                return BadRequest(new ApiResponse<Product>
                {
                    Success = false,
                    Message = "User not found"
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate user {UserId}", dto.UserId);
            return BadRequest(new ApiResponse<Product>
            {
                Success = false,
                Message = "Failed to validate user"
            });
        }

        var product = new Product
        {
            Id = Guid.NewGuid().ToString(),
            Name = dto.Name,
            Description = dto.Description,
            Price = dto.Price,
            UserId = dto.UserId,
            Category = dto.Category
        };

        Products.Add(product);

        return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, new ApiResponse<Product>
        {
            Success = true,
            Message = "Product created successfully",
            Data = product
        });
    }

    [HttpPut("{id}")]
    public ActionResult<ApiResponse<Product>> UpdateProduct(string id, [FromBody] UpdateProductDto dto)
    {
        _logger.LogInformation("Updating product with ID: {ProductId}", id);
        
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return BadRequest(new ApiResponse<Product>
            {
                Success = false,
                Message = "Validation failed",
                Errors = errors
            });
        }

        var product = Products.FirstOrDefault(p => p.Id == id);
        
        if (product == null)
        {
            return NotFound(new ApiResponse<Product>
            {
                Success = false,
                Message = "Product not found"
            });
        }

        product.Name = dto.Name;
        product.Description = dto.Description;
        product.Price = dto.Price;
        product.Category = dto.Category;
        product.UpdatedAt = DateTime.UtcNow;

        return Ok(new ApiResponse<Product>
        {
            Success = true,
            Message = "Product updated successfully",
            Data = product
        });
    }

    [HttpDelete("{id}")]
    public ActionResult<ApiResponse<object>> DeleteProduct(string id)
    {
        _logger.LogInformation("Deleting product with ID: {ProductId}", id);
        
        var product = Products.FirstOrDefault(p => p.Id == id);
        
        if (product == null)
        {
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = "Product not found"
            });
        }

        Products.Remove(product);

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = "Product deleted successfully"
        });
    }

    [HttpGet("health")]
    public ActionResult<object> HealthCheck()
    {
        return Ok(new { status = "healthy", service = "ProductService", timestamp = DateTime.UtcNow });
    }
}

public class CreateProductDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string? Category { get; set; }
}

public class UpdateProductDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string? Category { get; set; }
}