namespace MemoryAtelierBackend.DTOs;

public enum PurgeResult { NotFound, HasReferences, Purged }

public record RegisterDto(string Name, string Email, string Password, string? PhoneNumber, DateTime? BirthDate);
public record LoginDto(string Email, string Password);
public record AuthResponseDto(string Token, string Role, string Name, Guid UserId, string? Phone);

public record CategoryDto(Guid Id, string Name, string? NameEn, Guid? ParentId, List<CategoryDto> Children);
public record CreateCategoryDto(string Name, string? NameEn, Guid? ParentId);
public record UpdateCategoryDto(string Name, string? NameEn, Guid? ParentId);
public record ReorderCategoriesDto(List<Guid> OrderedIds);

public record ProductImageDto(Guid Id, string ImageUrl, int Order);

public record CategoryRefDto(Guid Id, string Name, string? NameEn);

public record CreateProductDto(
    string Name,
    string? NameEn,
    List<Guid> CategoryIds,
    decimal Price,
    string? Description,
    string? DescriptionEn,
    List<string> ImageUrls,
    int Stock
);

public record UpdateProductDto(
    string Name,
    string? NameEn,
    List<Guid> CategoryIds,
    decimal Price,
    string? Description,
    string? DescriptionEn,
    List<string> ImageUrls,
    int Stock
);

public record ProductDto(
    Guid Id,
    string Name,
    string? NameEn,
    string Slug,
    List<CategoryRefDto> Categories,
    decimal Price,
    string? Description,
    string? DescriptionEn,
    List<ProductImageDto> Images,
    int Stock,
    DateTime CreatedAt
);

public record UpdateProductStockDto(int Stock);

public record TrashedProductDto(Guid Id, string Name, string? NameEn, string? ImageUrl, decimal Price, DateTime DeletedAt);
public record TrashedCategoryDto(Guid Id, string Name, string? NameEn, DateTime DeletedAt, List<TrashedCategoryDto> Children);

public record CartItemDto(Guid Id, Guid ProductId, string ProductName, string? ImageUrl, decimal Price, int Quantity, decimal Total, int Stock, string? FulfillmentChoice);
public record AddToCartDto(Guid ProductId, int Quantity);
public record UpdateCartDto(int Quantity, string? FulfillmentChoice = null);
public record FavouriteDto(Guid Id, Guid ProductId, string ProductName, string? ImageUrl, decimal Price);

public record CreateOrderItemDto(Guid ProductId, int Quantity, string? FulfillmentChoice = null);

public record CreateOrderDto(
    string DeliveryAddress,
    string? DeliveryMethod,
    string DeliveryFirstName,
    string DeliveryLastName,
    string DeliveryPhone,
    string? Notes,
    string PaymentMethod,
    List<CreateOrderItemDto> Items
);

public record OrderResponseDto(Guid Id);

public record AdminOrderItemDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    decimal UnitPrice,
    int Quantity,
    string? FulfillmentChoice,
    int? ShippedNowQuantity,
    DateTime? FirstShipmentSentAt,
    DateTime? NextShipmentEstimatedAt,
    DateTime? FinalShipmentSentAt
);

public record AdminOrderDto(
    Guid Id,
    string CustomerName,
    string CustomerEmail,
    string DeliveryFirstName,
    string DeliveryLastName,
    string DeliveryPhone,
    string DeliveryAddress,
    string DeliveryMethod,
    string PaymentMethod,
    string Status,
    string? CancellationNote,
    bool IsSeen,
    decimal TotalAmount,
    DateTime CreatedAt,
    List<AdminOrderItemDto> Items
);

public record UpdateOrderStatusDto(string Status, string? Note = null);
public record UpdateShipmentInfoDto(DateTime? FirstShipmentSentAt, DateTime? NextShipmentEstimatedAt, DateTime? FinalShipmentSentAt);

public record MostFavouritedProductDto(string Name, int FavouriteCount);

public record LowStockProductDto(Guid Id, string Name, string? NameEn, List<CategoryRefDto> Categories, int Stock, string? ImageUrl);

public record DashboardStatsDto(
    int TotalProducts,
    int TotalCategories,
    int TotalOrders,
    decimal TotalRevenue,
    int NewUsersThisWeek,
    MostFavouritedProductDto? MostFavouritedProduct,
    List<LowStockProductDto> LowStockProducts
);

public record RevenueByMonthDto(string Month, decimal Revenue);

public record SalesByCategoryDto(string Category, int Count);

public record NewUsersByWeekDto(string Week, int Count);

public record DashboardChartsDto(
    List<RevenueByMonthDto> RevenueByMonth,
    List<SalesByCategoryDto> SalesByCategory,
    List<NewUsersByWeekDto> NewUsersByWeek
);

public record OrderItemInfo(string ProductName, int Quantity, decimal Price, string? FulfillmentChoice = null, int? ShippedNowQuantity = null);

public record HeroImageDto(Guid Id, string ImageUrl, int SortOrder);
public record CreateHeroImageDto(string ImageUrl);
public record ReorderHeroImagesDto(List<Guid> OrderedIds);

public record ContactMessageDto(string Name, string Email, string Message, string? Website = null);

public record ContactMessageSummaryDto(Guid Id, string Name, string Email, string Message, DateTime CreatedAt, bool IsReplied, string? ReplyText, DateTime? RepliedAt);
public record ReplyContactMessageDto(string ReplyText);

public class EmailSettings
{
    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; }
    public string From { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string AdminEmail { get; set; } = string.Empty;
}

public class SupabaseSettings
{
    public string ProjectUrl { get; set; } = string.Empty;
    public string AnonKey { get; set; } = string.Empty;
    public string ServiceRoleKey { get; set; } = string.Empty;
    public string StorageBucket { get; set; } = "product-images";
    public string DatabaseUrl { get; set; } = string.Empty;
}

public class BankTransferSettings
{
    public string Iban { get; set; } = string.Empty;
    public string AccountHolder { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
}

public class ReviewDto
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class CreateReviewDto
{
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
}
