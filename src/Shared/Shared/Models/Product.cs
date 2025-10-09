using System.ComponentModel.DataAnnotations;

namespace GoNomads.Shared.Models;

public class Product
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;
    
    [StringLength(1000)]
    public string? Description { get; set; }
    
    [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
    public decimal Price { get; set; }
    
    [Required]
    public string UserId { get; set; } = string.Empty;
    
    [StringLength(100)]
    public string? Category { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}