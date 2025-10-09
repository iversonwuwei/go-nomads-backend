using Grpc.Core;
using Grpc.Net.Client;
using GoNomads.Shared.Grpc;
using Dapr.Client;
using DomainProduct = GoNomads.Shared.Models.Product;
using GrpcProduct = GoNomads.Shared.Grpc.Product;

namespace ProductService.Services;

public class ProductGrpcService : GoNomads.Shared.Grpc.ProductService.ProductServiceBase
{
    private static readonly List<DomainProduct> Products = new();
    private readonly ILogger<ProductGrpcService> _logger;
    private readonly DaprClient _daprClient;

    public ProductGrpcService(ILogger<ProductGrpcService> logger, DaprClient daprClient)
    {
        _logger = logger;
        _daprClient = daprClient;
        
        // Initialize with sample data
        if (!Products.Any())
        {
            Products.AddRange(new[]
            {
                new DomainProduct 
                { 
                    Id = "1", 
                    Name = "Laptop", 
                    Description = "High-performance laptop", 
                    Price = 999.99m, 
                    UserId = "1", 
                    Category = "Electronics" 
                },
                new DomainProduct 
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

    public override Task<ProductResponse> GetProduct(GetProductRequest request, ServerCallContext context)
    {
        _logger.LogInformation("Getting product with ID: {ProductId}", request.Id);
        
        var product = Products.FirstOrDefault(p => p.Id == request.Id);
        
        if (product == null)
        {
            return Task.FromResult(new ProductResponse
            {
                Success = false,
                Message = "Product not found"
            });
        }

        return Task.FromResult(new ProductResponse
        {
            Product = new GrpcProduct
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description ?? "",
                Price = (double)product.Price,
                UserId = product.UserId,
                Category = product.Category ?? "",
                CreatedAt = product.CreatedAt.Ticks,
                UpdatedAt = product.UpdatedAt.Ticks
            },
            Success = true,
            Message = "Product retrieved successfully"
        });
    }

    public override async Task<ProductResponse> CreateProduct(CreateProductRequest request, ServerCallContext context)
    {
        _logger.LogInformation("Creating product: {ProductName}", request.Name);
        
        // Validate user exists by calling UserService via Dapr
        try
        {
            var userResponse = await _daprClient.InvokeMethodAsync<GetUserRequest, UserResponse>(
                "user-service",
                "GetUser",
                new GetUserRequest { Id = request.UserId });
                
            if (!userResponse.Success)
            {
                return new ProductResponse
                {
                    Success = false,
                    Message = "User not found"
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating user {UserId}", request.UserId);
            return new ProductResponse
            {
                Success = false, 
                Message = "Failed to validate user"
            };
        }

        var product = new DomainProduct
        {
            Id = Guid.NewGuid().ToString(),
            Name = request.Name,
            Description = request.Description,
            Price = (decimal)request.Price,
            UserId = request.UserId,
            Category = request.Category
        };

        Products.Add(product);

        return new ProductResponse
        {
            Product = new GrpcProduct
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description ?? "",
                Price = (double)product.Price,
                UserId = product.UserId,
                Category = product.Category ?? "",
                CreatedAt = product.CreatedAt.Ticks,
                UpdatedAt = product.UpdatedAt.Ticks
            },
            Success = true,
            Message = "Product created successfully"
        };
    }

    public override Task<ProductResponse> UpdateProduct(UpdateProductRequest request, ServerCallContext context)
    {
        _logger.LogInformation("Updating product with ID: {ProductId}", request.Id);
        
        var product = Products.FirstOrDefault(p => p.Id == request.Id);
        
        if (product == null)
        {
            return Task.FromResult(new ProductResponse
            {
                Success = false,
                Message = "Product not found"
            });
        }

        // Update product properties
        product.Name = request.Name;
        product.Description = request.Description;
        product.Price = (decimal)request.Price;
        product.Category = request.Category;
        product.UpdatedAt = DateTime.UtcNow;

        return Task.FromResult(new ProductResponse
        {
            Product = new GrpcProduct
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description ?? "",
                Price = (double)product.Price,
                UserId = product.UserId,
                Category = product.Category ?? "",
                CreatedAt = product.CreatedAt.Ticks,
                UpdatedAt = product.UpdatedAt.Ticks
            },
            Success = true,
            Message = "Product updated successfully"
        });
    }

    public override Task<DeleteProductResponse> DeleteProduct(DeleteProductRequest request, ServerCallContext context)
    {
        _logger.LogInformation("Deleting product with ID: {ProductId}", request.Id);
        
        var product = Products.FirstOrDefault(p => p.Id == request.Id);
        
        if (product == null)
        {
            return Task.FromResult(new DeleteProductResponse
            {
                Success = false,
                Message = "Product not found"
            });
        }

        Products.Remove(product);
        
        return Task.FromResult(new DeleteProductResponse
        {
            Success = true,
            Message = "Product deleted successfully"
        });
    }

    public override Task<ListProductsResponse> ListProducts(ListProductsRequest request, ServerCallContext context)
    {
        _logger.LogInformation("Listing products");
        
        var skip = Math.Max(0, (request.Page - 1) * request.PageSize);  
        var take = Math.Min(request.PageSize, 100); // Max 100 items per page
        
        var products = Products.Skip(skip).Take(take);
        
        var response = new ListProductsResponse
        {
            Success = true,
            Message = "Products retrieved successfully",
            TotalCount = Products.Count
        };

        foreach (var product in products)
        {
            response.Products.Add(new GrpcProduct
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description ?? "",
                Price = (double)product.Price,
                UserId = product.UserId,
                Category = product.Category ?? "",
                CreatedAt = product.CreatedAt.Ticks,
                UpdatedAt = product.UpdatedAt.Ticks
            });
        }

        return Task.FromResult(response);
    }

    public override Task<ListProductsResponse> GetProductsByUserId(GetProductsByUserIdRequest request, ServerCallContext context)
    {
        _logger.LogInformation("Getting products for user ID: {UserId}", request.UserId);
        
        var userProducts = Products.Where(p => p.UserId == request.UserId);
        
        var skip = Math.Max(0, (request.Page - 1) * request.PageSize);
        var take = Math.Min(request.PageSize, 100);
        
        var products = userProducts.Skip(skip).Take(take);
        
        var response = new ListProductsResponse
        {
            Success = true,
            Message = "User products retrieved successfully",
            TotalCount = userProducts.Count()
        };

        foreach (var product in products)
        {
            response.Products.Add(new GrpcProduct
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description ?? "",
                Price = (double)product.Price,
                UserId = product.UserId,
                Category = product.Category ?? "",
                CreatedAt = product.CreatedAt.Ticks,
                UpdatedAt = product.UpdatedAt.Ticks
            });
        }

        return Task.FromResult(response);
    }
}