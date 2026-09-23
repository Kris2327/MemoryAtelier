namespace MemoryAtelierBackend.Models;

public class User
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = "Client";
    public string? PhoneNumber { get; set; }
    public DateTime? BirthDate { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class Category
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? NameEn { get; set; }
    public string? Description { get; set; }
    public string? DescriptionEn { get; set; }
    public Guid? ParentId { get; set; }
    public Category? Parent { get; set; }
    public List<Category> Children { get; set; } = new();
    public List<Product> Products { get; set; } = new();
    public int SortOrder { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    // Скрита от публичния сайт (но не изтрита) — каскадно важи и за поддървото/продуктите ѝ, вж. CategoryService.HideAsync/ShowAsync
    public bool IsHidden { get; set; }
}

public class Product
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? NameEn { get; set; }
    // URL-safe четим идентификатор за SEO (напр. "raka-pletena-nagrudnitsa") — идентификацията си остава по Id, slug-ът е само декоративен в URL-а
    public string Slug { get; set; } = string.Empty;
    public List<Category> Categories { get; set; } = new();
    public decimal Price { get; set; }
    public string? Description { get; set; }
    public string? DescriptionEn { get; set; }
    public int Stock { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<ProductImage> Images { get; set; } = new();
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    // Скрит от публичния сайт (но не изтрит). Не каскадира от категория — видимостта му спрямо скрита секция се смята динамично, вж. ProductService.GetAllAsync
    public bool IsHidden { get; set; }
}

public class ProductImage
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public Product? Product { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public int Order { get; set; }
}

public class CartItem
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public Guid ProductId { get; set; }
    public Product? Product { get; set; }
    public int Quantity { get; set; }
    // Само когато Quantity > Product.Stock: "Split" (две пратки) или "Wait" (изчакване за пълна доставка наведнъж)
    public string? FulfillmentChoice { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class Favourite
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public Guid ProductId { get; set; }
    public Product? Product { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class Review
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public Product Product { get; set; } = null!;
}

public class Order
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public string DeliveryFirstName { get; set; } = string.Empty;
    public string DeliveryLastName { get; set; } = string.Empty;
    public string DeliveryPhone { get; set; } = string.Empty;
    public string DeliveryAddress { get; set; } = string.Empty;
    public string DeliveryMethod { get; set; } = "Address";
    public string? Notes { get; set; }
    public string PaymentMethod { get; set; } = "CashOnDelivery";
    public string Status { get; set; } = "Pending";
    // Бележка от админа при отказване на поръчката — включва се в имейла до клиента за отказа
    public string? CancellationNote { get; set; }
    // Дали админ вече е отворил детайлите на поръчката — за нотификация "нова поръчка" в админ панела
    public bool IsSeen { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<OrderItem> Items { get; set; } = new();
}

public class OrderItem
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Order? Order { get; set; }
    public Guid ProductId { get; set; }
    public Product? Product { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    // Само когато поръчаното количество е надвишавало наличността в момента на поръчката: "Split" или "Wait"
    public string? FulfillmentChoice { get; set; }
    // Само за FulfillmentChoice == "Split": колко от Quantity са били налични и се изпращат веднага (остатъкът = Quantity - ShippedNowQuantity)
    public int? ShippedNowQuantity { get; set; }
    // Само за FulfillmentChoice == "Split": кога е изпратена първата пратка (наличното количество)
    public DateTime? FirstShipmentSentAt { get; set; }
    // За "Split": до кога се очаква да бъде изпратена втората пратка (остатъкът). За "Wait": до кога се очаква да бъде изпратена цялата поръчка.
    public DateTime? NextShipmentEstimatedAt { get; set; }
    // Реалната дата на последната/финалната пратка: за "Split" — втората (последна) пратка; за "Wait" — единствената пратка.
    // При попълване се изпраща отделен "изпратено" имейл до клиента и (ако това е последният чакащ артикул) статусът на цялата поръчка автоматично минава на "Shipped".
    public DateTime? FinalShipmentSentAt { get; set; }
}

public class HeroImage
{
    public Guid Id { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ContactMessage
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsReplied { get; set; }
    public string? ReplyText { get; set; }
    public DateTime? RepliedAt { get; set; }
}
