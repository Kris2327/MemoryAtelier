using Microsoft.EntityFrameworkCore;
using MemoryAtelierBackend.Data;
using MemoryAtelierBackend.DTOs;
using MemoryAtelierBackend.Models;

namespace MemoryAtelierBackend.Services;

public class OrderService(
    AppDbContext db,
    EmailService emailService,
    CartService cartService,
    ILogger<OrderService> logger)
{
    public async Task<(OrderResponseDto? Result, string? Error)> CreateAsync(Guid userId, CreateOrderDto dto)
    {
        if (dto.Items == null || dto.Items.Count == 0)
        {
            return (null, "Order must include at least one item.");
        }

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
        {
            return (null, "User not found.");
        }

        var requestedQuantities = dto.Items
            .GroupBy(item => item.ProductId)
            .ToDictionary(group => group.Key, group => group.Sum(item => item.Quantity));

        var fulfillmentChoices = dto.Items
            .Where(item => !string.IsNullOrWhiteSpace(item.FulfillmentChoice))
            .GroupBy(item => item.ProductId)
            .ToDictionary(group => group.Key, group => group.First().FulfillmentChoice);

        if (requestedQuantities.Values.Any(quantity => quantity <= 0))
        {
            return (null, "All item quantities must be positive.");
        }

        var productIds = requestedQuantities.Keys.ToList();
        var products = await db.Products
            .Where(product => productIds.Contains(product.Id))
            .ToDictionaryAsync(product => product.Id);

        if (products.Count != productIds.Count)
        {
            return (null, "One or more products do not exist.");
        }

        // Поръчка над наличното количество се позволява (частична доставка/изчакване до 4 седмици),
        // затова тук не отхвърляме поръчката — наличността просто не пада под 0.

        var order = new Order
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            DeliveryAddress = dto.DeliveryAddress.Trim(),
            DeliveryMethod = string.IsNullOrWhiteSpace(dto.DeliveryMethod) ? "Address" : dto.DeliveryMethod.Trim(),
            DeliveryFirstName = dto.DeliveryFirstName.Trim(),
            DeliveryLastName = dto.DeliveryLastName.Trim(),
            DeliveryPhone = dto.DeliveryPhone.Trim(),
            Notes = string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes.Trim(),
            PaymentMethod = string.IsNullOrWhiteSpace(dto.PaymentMethod) ? "CashOnDelivery" : dto.PaymentMethod.Trim(),
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };

        foreach (var (productId, quantity) in requestedQuantities)
        {
            var product = products[productId];
            var availableAtOrderTime = product.Stock;
            product.Stock = Math.Max(0, product.Stock - quantity);

            var fulfillmentChoice = fulfillmentChoices.GetValueOrDefault(productId);
            var shippedNowQuantity = fulfillmentChoice == "Split"
                ? Math.Clamp(availableAtOrderTime, 0, quantity)
                : (int?)null;

            order.Items.Add(new OrderItem
            {
                Id = Guid.NewGuid(),
                ProductId = product.Id,
                ProductName = product.Name,
                UnitPrice = product.Price,
                Quantity = quantity,
                FulfillmentChoice = fulfillmentChoice,
                ShippedNowQuantity = shippedNowQuantity
            });
        }

        order.TotalAmount = order.Items.Sum(item => item.UnitPrice * item.Quantity);

        db.Orders.Add(order);
        await db.SaveChangesAsync();

        await cartService.ClearCartAsync(userId);

        var customerName = $"{order.DeliveryFirstName} {order.DeliveryLastName}".Trim();
        var emailItems = order.Items
            .Select(item => new OrderItemInfo(item.ProductName, item.Quantity, item.UnitPrice, item.FulfillmentChoice, item.ShippedNowQuantity))
            .ToList();

        try
        {
            await emailService.SendOrderNotificationAsync(
                customerName,
                user.Email,
                order.DeliveryPhone,
                order.DeliveryAddress,
                order.DeliveryMethod,
                order.PaymentMethod,
                emailItems,
                order.TotalAmount
            );
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Order {OrderId} was created, but notification email could not be sent.", order.Id);
        }

        try
        {
            await emailService.SendOrderConfirmationAsync(
                customerName,
                user.Email,
                order.Id,
                order.DeliveryAddress,
                order.DeliveryMethod,
                order.PaymentMethod,
                emailItems,
                order.TotalAmount
            );
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Order {OrderId} was created, but order confirmation email could not be sent.", order.Id);
        }

        return (new OrderResponseDto(order.Id), null);
    }

    public async Task<List<AdminOrderDto>> GetAllAsync()
    {
        var orders = await db.Orders
            .Include(order => order.User)
            .Include(order => order.Items)
            .OrderByDescending(order => order.CreatedAt)
            .ToListAsync();

        return orders.Select(ToAdminOrderDto).ToList();
    }

    public async Task<bool> MarkSeenAsync(Guid orderId)
    {
        var order = await db.Orders.FirstOrDefaultAsync(o => o.Id == orderId);
        if (order == null) return false;

        if (!order.IsSeen)
        {
            order.IsSeen = true;
            await db.SaveChangesAsync();
        }

        return true;
    }

    public async Task<bool> UpdateStatusAsync(Guid orderId, string status, string? note)
    {
        var order = await db.Orders
            .Include(o => o.User)
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId);
        if (order == null) return false;

        // За поръчки с "Раздели"/"Изчакай" продукти статус "Shipped" се определя автоматично (виж UpdateItemShipmentInfoAsync) —
        // ръчно задаване тук би било подвеждащо, докато има чакащи пратки, затова не се позволява.
        if (status == "Shipped" && HasPendingFulfillmentItems(order))
        {
            return false;
        }

        var previousStatus = order.Status;
        order.Status = status;

        if (status == "Cancelled")
        {
            order.CancellationNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        }

        await db.SaveChangesAsync();

        if (order.User != null)
        {
            var customerName = $"{order.DeliveryFirstName} {order.DeliveryLastName}".Trim();

            if (status == "Shipped" && previousStatus != "Shipped")
            {
                try
                {
                    await emailService.SendOrderShippedAsync(customerName, order.User.Email, order.Id, order.DeliveryMethod, order.DeliveryAddress);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Order {OrderId} status set to Shipped, but the notification email could not be sent.", order.Id);
                }
            }
            else if (status == "Cancelled" && previousStatus != "Cancelled")
            {
                try
                {
                    await emailService.SendOrderCancelledAsync(customerName, order.User.Email, order.Id, order.CancellationNote);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Order {OrderId} status set to Cancelled, but the notification email could not be sent.", order.Id);
                }
            }
        }

        return true;
    }

    public async Task<bool> UpdateItemShipmentInfoAsync(Guid orderId, Guid itemId, UpdateShipmentInfoDto dto)
    {
        var order = await db.Orders
            .Include(o => o.User)
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId);
        if (order == null) return false;

        var item = order.Items.FirstOrDefault(i => i.Id == itemId);
        if (item == null || string.IsNullOrWhiteSpace(item.FulfillmentChoice)) return false;

        var previousFinalShipmentSentAt = item.FinalShipmentSentAt;

        item.FirstShipmentSentAt = AsUtc(dto.FirstShipmentSentAt);
        item.NextShipmentEstimatedAt = AsUtc(dto.NextShipmentEstimatedAt);
        item.FinalShipmentSentAt = AsUtc(dto.FinalShipmentSentAt);

        // щом последната чакаща пратка в поръчката е отбелязана като изпратена, цялата поръчка автоматично минава на "Shipped"
        var finalShipmentJustSent = item.FinalShipmentSentAt.HasValue && item.FinalShipmentSentAt != previousFinalShipmentSentAt;
        var orderNowFullyShipped = finalShipmentJustSent && !HasPendingFulfillmentItems(order) && order.Status != "Cancelled";
        if (orderNowFullyShipped)
        {
            order.Status = "Shipped";
        }

        await db.SaveChangesAsync();

        if (order.User != null)
        {
            var customerName = $"{order.DeliveryFirstName} {order.DeliveryLastName}".Trim();
            try
            {
                if (finalShipmentJustSent)
                {
                    var isSplitRemainder = item.FulfillmentChoice == "Split";
                    var quantity = isSplitRemainder ? item.Quantity - (item.ShippedNowQuantity ?? 0) : item.Quantity;
                    await emailService.SendFinalShipmentSentAsync(customerName, order.User.Email, order.Id, item.ProductName, quantity, isSplitRemainder);
                }
                else
                {
                    await emailService.SendShipmentTimelineUpdateAsync(
                        customerName,
                        order.User.Email,
                        order.Id,
                        item.ProductName,
                        item.FulfillmentChoice,
                        item.Quantity,
                        item.ShippedNowQuantity,
                        item.FirstShipmentSentAt,
                        item.NextShipmentEstimatedAt
                    );
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Order {OrderId} shipment info for item {ItemId} was updated, but the notification email could not be sent.", order.Id, item.Id);
            }
        }

        return true;
    }

    // "чакащ" = продукт с избор Split/Wait, за който все още не е отбелязана реалната дата на финалната му пратка
    private static bool HasPendingFulfillmentItems(Order order) =>
        order.Items.Any(item => !string.IsNullOrWhiteSpace(item.FulfillmentChoice) && !item.FinalShipmentSentAt.HasValue);

    // датите идват от <input type="date"> без часова зона (Kind=Unspecified) — Postgres изисква UTC за "timestamp with time zone"
    private static DateTime? AsUtc(DateTime? date) =>
        date.HasValue ? DateTime.SpecifyKind(date.Value, DateTimeKind.Utc) : null;

    private static AdminOrderDto ToAdminOrderDto(Order order) => new(
        order.Id,
        $"{order.DeliveryFirstName} {order.DeliveryLastName}".Trim(),
        order.User?.Email ?? "",
        order.DeliveryFirstName,
        order.DeliveryLastName,
        order.DeliveryPhone,
        order.DeliveryAddress,
        order.DeliveryMethod,
        order.PaymentMethod,
        order.Status,
        order.CancellationNote,
        order.IsSeen,
        order.TotalAmount,
        order.CreatedAt,
        order.Items.Select(item => new AdminOrderItemDto(
            item.Id,
            item.ProductId,
            item.ProductName,
            item.UnitPrice,
            item.Quantity,
            item.FulfillmentChoice,
            item.ShippedNowQuantity,
            item.FirstShipmentSentAt,
            item.NextShipmentEstimatedAt,
            item.FinalShipmentSentAt
        )).ToList()
    );
}
