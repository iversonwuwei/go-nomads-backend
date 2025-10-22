using System.ComponentModel.DataAnnotations;
using Postgrest.Attributes;
using Postgrest.Models;

namespace EcommerceService.Models;

/// <summary>
/// 商品实体模型
/// </summary>
[Table("products")]
public class Product : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(200)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [MaxLength(50)]
    [Column("category")]
    public string? Category { get; set; } // gear, accessories, books, courses, services, other

    [Required]
    [Column("price")]
    public decimal Price { get; set; }

    [MaxLength(10)]
    [Column("currency")]
    public string Currency { get; set; } = "USD";

    [Column("stock")]
    public int Stock { get; set; } = 0;

    [Column("images")]
    public string[]? Images { get; set; }

    [Column("tags")]
    public string[]? Tags { get; set; }

    [Column("rating")]
    public decimal Rating { get; set; } = 0;

    [Column("review_count")]
    public int ReviewCount { get; set; } = 0;

    [Column("is_featured")]
    public bool IsFeatured { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("seller_id")]
    public Guid? SellerId { get; set; }

    [Column("created_by")]
    public Guid? CreatedBy { get; set; }

    [Column("updated_by")]
    public Guid? UpdatedBy { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 购物车项实体模型
/// </summary>
[Table("cart_items")]
public class CartItem : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Required]
    [Column("user_id")]
    public Guid UserId { get; set; }

    [Required]
    [Column("product_id")]
    public Guid ProductId { get; set; }

    [Column("quantity")]
    public int Quantity { get; set; } = 1;

    [Column("added_at")]
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 订单实体模型
/// </summary>
[Table("orders")]
public class Order : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Required]
    [Column("user_id")]
    public Guid UserId { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("order_number")]
    public string OrderNumber { get; set; } = string.Empty;

    [Required]
    [Column("total_amount")]
    public decimal TotalAmount { get; set; }

    [MaxLength(10)]
    [Column("currency")]
    public string Currency { get; set; } = "USD";

    [MaxLength(20)]
    [Column("status")]
    public string Status { get; set; } = "pending"; // pending, processing, shipped, delivered, cancelled, refunded

    [MaxLength(20)]
    [Column("payment_status")]
    public string PaymentStatus { get; set; } = "pending"; // pending, paid, failed, refunded

    [MaxLength(50)]
    [Column("payment_method")]
    public string? PaymentMethod { get; set; }

    [Column("shipping_address")]
    public string? ShippingAddress { get; set; }

    [MaxLength(100)]
    [Column("tracking_number")]
    public string? TrackingNumber { get; set; }

    [Column("notes")]
    public string? Notes { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 订单项实体模型
/// </summary>
[Table("order_items")]
public class OrderItem : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Required]
    [Column("order_id")]
    public Guid OrderId { get; set; }

    [Column("product_id")]
    public Guid? ProductId { get; set; }

    [Required]
    [MaxLength(200)]
    [Column("product_name")]
    public string ProductName { get; set; } = string.Empty;

    [Required]
    [Column("product_price")]
    public decimal ProductPrice { get; set; }

    [Required]
    [Column("quantity")]
    public int Quantity { get; set; }

    [Required]
    [Column("subtotal")]
    public decimal Subtotal { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
