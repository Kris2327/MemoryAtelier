using System.Globalization;
using System.Text;
using MailKit.Net.Smtp;
using MailKit.Security;
using MemoryAtelierBackend.DTOs;
using Microsoft.Extensions.Options;
using MimeKit;

namespace MemoryAtelierBackend.Services;

public class EmailService(IOptions<EmailSettings> emailOptions, IOptions<BankTransferSettings> bankOptions, ILogger<EmailService> logger)
{
    private readonly EmailSettings _settings = emailOptions.Value;
    private readonly BankTransferSettings _bank = bankOptions.Value;

    public async Task SendContactMessageAsync(string name, string email, string messageText)
    {
        if (string.IsNullOrWhiteSpace(_settings.AdminEmail))
        {
            throw new InvalidOperationException("Email:AdminEmail is not configured.");
        }

        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(_settings.From));
        message.To.Add(MailboxAddress.Parse(_settings.AdminEmail));
        message.ReplyTo.Add(MailboxAddress.Parse(email));
        message.Subject = $"Ново съобщение от {name} (контактна форма)";

        var builder = new BodyBuilder
        {
            HtmlBody = BuildContactMessageHtml(name, email, messageText)
        };

        message.Body = builder.ToMessageBody();

        using var smtp = new SmtpClient();
        await smtp.ConnectAsync(_settings.SmtpHost, _settings.SmtpPort, SecureSocketOptions.StartTls);
        await smtp.AuthenticateAsync(_settings.From, _settings.Password);
        await smtp.SendAsync(message);
        await smtp.DisconnectAsync(true);

        logger.LogInformation("Contact form message sent from {Email}.", email);
    }

    private static string BuildContactMessageHtml(string name, string email, string messageText)
    {
        var safeMessage = System.Net.WebUtility.HtmlEncode(messageText).Replace("\n", "<br>");

        return $$"""
            <!DOCTYPE html>
            <html lang="bg">
            <head>
              <meta charset="utf-8">
              <title>Ново съобщение</title>
            </head>
            <body style="margin: 0; padding: 24px; background: #fdf8f4; font-family: Lato, Arial, sans-serif; color: #2c1810;">
              <div style="max-width: 640px; margin: 0 auto; background: #ffffff; border-radius: 24px; overflow: hidden; box-shadow: 0 16px 40px rgba(44, 24, 16, 0.12);">
                <div style="padding: 32px; background: linear-gradient(135deg, #2c1810 0%, #c97d4e 100%); color: #fff8f3;">
                  <div style="font-size: 13px; letter-spacing: 2px; text-transform: uppercase; opacity: 0.85;">Memory Atelier</div>
                  <h1 style="margin: 12px 0 8px; font-family: 'Playfair Display', Georgia, serif; font-size: 28px;">Ново съобщение от контактната форма</h1>
                </div>

                <div style="padding: 32px;">
                  <div style="margin-bottom: 24px; padding: 20px; background: #f7ede6; border-radius: 18px;">
                    <div style="font-size: 12px; text-transform: uppercase; letter-spacing: 1px; color: #8b4513; margin-bottom: 8px;">От</div>
                    <div style="font-size: 18px; font-weight: 700; margin-bottom: 4px;">{{name}}</div>
                    <div style="font-size: 15px; color: #5e4639;">{{email}}</div>
                  </div>

                  <div style="padding: 20px 24px; background: #f7ede6; border-radius: 18px; font-size: 15px; line-height: 1.7; color: #2c1810;">
                    {{safeMessage}}
                  </div>
                </div>
              </div>
            </body>
            </html>
            """;
    }

    public async Task SendContactReplyAsync(string name, string email, string originalMessage, string replyText)
    {
        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(_settings.From));
        message.To.Add(MailboxAddress.Parse(email));
        if (!string.IsNullOrWhiteSpace(_settings.AdminEmail))
        {
            message.ReplyTo.Add(MailboxAddress.Parse(_settings.AdminEmail));
        }
        message.Subject = "Отговор от Memory Atelier на вашето съобщение";

        var builder = new BodyBuilder
        {
            HtmlBody = BuildContactReplyHtml(name, originalMessage, replyText)
        };

        message.Body = builder.ToMessageBody();

        using var smtp = new SmtpClient();
        await smtp.ConnectAsync(_settings.SmtpHost, _settings.SmtpPort, SecureSocketOptions.StartTls);
        await smtp.AuthenticateAsync(_settings.From, _settings.Password);
        await smtp.SendAsync(message);
        await smtp.DisconnectAsync(true);

        logger.LogInformation("Contact reply email sent to {Email}.", email);
    }

    private static string BuildContactReplyHtml(string name, string originalMessage, string replyText)
    {
        var safeOriginal = System.Net.WebUtility.HtmlEncode(originalMessage).Replace("\n", "<br>");
        var safeReply = System.Net.WebUtility.HtmlEncode(replyText).Replace("\n", "<br>");

        return $$"""
            <!DOCTYPE html>
            <html lang="bg">
            <head>
              <meta charset="utf-8">
              <title>Отговор на съобщението ви</title>
            </head>
            <body style="margin: 0; padding: 24px; background: #fdf8f4; font-family: Lato, Arial, sans-serif; color: #2c1810;">
              <div style="max-width: 640px; margin: 0 auto; background: #ffffff; border-radius: 24px; overflow: hidden; box-shadow: 0 16px 40px rgba(44, 24, 16, 0.12);">
                <div style="padding: 32px; background: linear-gradient(135deg, #2c1810 0%, #c97d4e 100%); color: #fff8f3;">
                  <div style="font-size: 13px; letter-spacing: 2px; text-transform: uppercase; opacity: 0.85;">Memory Atelier</div>
                  <h1 style="margin: 12px 0 8px; font-family: 'Playfair Display', Georgia, serif; font-size: 28px;">Отговор на вашето съобщение</h1>
                  <p style="margin: 0; font-size: 15px; line-height: 1.6;">Здравейте, {{name}}! Ето нашия отговор на въпроса, който ни изпратихте.</p>
                </div>

                <div style="padding: 32px;">
                  <div style="padding: 20px 24px; background: #2c1810; border-radius: 18px; color: #fff8f3; font-size: 15px; line-height: 1.7;">
                    <div style="font-size: 12px; text-transform: uppercase; letter-spacing: 1px; opacity: 0.8; margin-bottom: 8px;">Нашият отговор</div>
                    {{safeReply}}
                  </div>

                  <div style="margin-top: 24px; padding: 20px 24px; background: #f7ede6; border-radius: 18px; font-size: 14px; line-height: 1.7; color: #5e4639;">
                    <div style="font-size: 12px; text-transform: uppercase; letter-spacing: 1px; color: #8b4513; margin-bottom: 8px;">Вашето съобщение</div>
                    {{safeOriginal}}
                  </div>
                </div>
              </div>
            </body>
            </html>
            """;
    }

    public async Task SendOrderNotificationAsync(
        string customerName,
        string customerEmail,
        string deliveryPhone,
        string deliveryAddress,
        string deliveryMethod,
        string paymentMethod,
        List<OrderItemInfo> items,
        decimal total)
    {
        if (string.IsNullOrWhiteSpace(_settings.AdminEmail))
        {
            throw new InvalidOperationException("Email:AdminEmail is not configured.");
        }

        // Gmail SMTP requires a Google App Password instead of the normal account password.
        // To generate it, enable 2-Factor Authentication on the Google account first.
        // Then go to Google Account -> Security -> App Passwords and create a Mail app password.
        // Store that generated 16-character password in Email:Password.

        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(_settings.From));
        message.To.Add(MailboxAddress.Parse(_settings.AdminEmail));
        message.Subject = $"🛒 Нова поръчка от {customerName}";

        var builder = new BodyBuilder
        {
            HtmlBody = BuildOrderHtml(customerName, customerEmail, deliveryPhone, deliveryAddress, deliveryMethod, paymentMethod, items, total)
        };

        message.Body = builder.ToMessageBody();

        using var smtp = new SmtpClient();
        await smtp.ConnectAsync(_settings.SmtpHost, _settings.SmtpPort, SecureSocketOptions.StartTls);
        await smtp.AuthenticateAsync(_settings.From, _settings.Password);
        await smtp.SendAsync(message);
        await smtp.DisconnectAsync(true);

        logger.LogInformation("Order notification email sent for customer {CustomerEmail}.", customerEmail);
    }

    public async Task SendOrderConfirmationAsync(
        string customerName,
        string customerEmail,
        Guid orderId,
        string deliveryAddress,
        string deliveryMethod,
        string paymentMethod,
        List<OrderItemInfo> items,
        decimal total)
    {
        if (paymentMethod == "BankTransfer" && string.IsNullOrWhiteSpace(_bank.Iban))
        {
            throw new InvalidOperationException("BankTransfer:Iban is not configured.");
        }

        var orderRef = orderId.ToString()[..8].ToUpperInvariant();

        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(_settings.From));
        message.To.Add(MailboxAddress.Parse(customerEmail));
        message.Subject = $"Потвърждение на поръчка №{orderRef}";

        var builder = new BodyBuilder
        {
            HtmlBody = BuildOrderConfirmationHtml(customerName, orderId, deliveryAddress, deliveryMethod, paymentMethod, items, total)
        };

        message.Body = builder.ToMessageBody();

        using var smtp = new SmtpClient();
        await smtp.ConnectAsync(_settings.SmtpHost, _settings.SmtpPort, SecureSocketOptions.StartTls);
        await smtp.AuthenticateAsync(_settings.From, _settings.Password);
        await smtp.SendAsync(message);
        await smtp.DisconnectAsync(true);

        logger.LogInformation("Order confirmation email sent for order {OrderId} to {CustomerEmail}.", orderId, customerEmail);
    }

    public async Task SendOrderShippedAsync(string customerName, string customerEmail, Guid orderId, string deliveryMethod, string deliveryAddress)
    {
        var orderRef = orderId.ToString()[..8].ToUpperInvariant();
        var deliveryMethodLabel = deliveryMethod == "SpeedyOffice" ? "До офис на Speedy" : "До адрес";

        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(_settings.From));
        message.To.Add(MailboxAddress.Parse(customerEmail));
        message.Subject = $"Поръчка №{orderRef} е изпратена!";

        var builder = new BodyBuilder
        {
            HtmlBody = BuildOrderShippedHtml(customerName, orderRef, deliveryMethodLabel, deliveryAddress)
        };

        message.Body = builder.ToMessageBody();

        using var smtp = new SmtpClient();
        await smtp.ConnectAsync(_settings.SmtpHost, _settings.SmtpPort, SecureSocketOptions.StartTls);
        await smtp.AuthenticateAsync(_settings.From, _settings.Password);
        await smtp.SendAsync(message);
        await smtp.DisconnectAsync(true);

        logger.LogInformation("Order shipped email sent for order {OrderId} to {CustomerEmail}.", orderId, customerEmail);
    }

    private static string BuildOrderShippedHtml(string customerName, string orderRef, string deliveryMethodLabel, string deliveryAddress)
    {
        return $$"""
            <!DOCTYPE html>
            <html lang="bg">
            <head>
              <meta charset="utf-8">
              <title>Поръчката е изпратена</title>
            </head>
            <body style="margin: 0; padding: 24px; background: #fdf8f4; font-family: Lato, Arial, sans-serif; color: #2c1810;">
              <div style="max-width: 640px; margin: 0 auto; background: #ffffff; border-radius: 24px; overflow: hidden; box-shadow: 0 16px 40px rgba(44, 24, 16, 0.12);">
                <div style="padding: 32px; background: linear-gradient(135deg, #2c1810 0%, #c97d4e 100%); color: #fff8f3;">
                  <div style="font-size: 13px; letter-spacing: 2px; text-transform: uppercase; opacity: 0.85;">Memory Atelier</div>
                  <h1 style="margin: 12px 0 8px; font-family: 'Playfair Display', Georgia, serif; font-size: 30px;">📦 Поръчката е изпратена!</h1>
                  <p style="margin: 0; font-size: 15px; line-height: 1.6;">Здравейте, {{customerName}}! Поръчка №{{orderRef}} е на път към вас.</p>
                </div>

                <div style="padding: 32px;">
                  <div style="padding: 20px 24px; background: #f7ede6; border-radius: 18px;">
                    <div style="font-size: 12px; text-transform: uppercase; letter-spacing: 1px; color: #8b4513; margin-bottom: 8px;">Данни за доставка</div>
                    <div style="font-size: 15px; font-weight: 700; margin-bottom: 4px;">{{deliveryMethodLabel}}</div>
                    <div style="font-size: 15px; color: #5e4639;">{{deliveryAddress}}</div>
                  </div>

                  <p style="margin-top: 24px; font-size: 14px; color: #5e4639; line-height: 1.7;">
                    Благодарим ви, че пазарувате от Memory Atelier!
                  </p>
                </div>
              </div>
            </body>
            </html>
            """;
    }

    public async Task SendOrderCancelledAsync(string customerName, string customerEmail, Guid orderId, string? note)
    {
        var orderRef = orderId.ToString()[..8].ToUpperInvariant();

        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(_settings.From));
        message.To.Add(MailboxAddress.Parse(customerEmail));
        message.Subject = $"Поръчка №{orderRef} е отказана";

        var builder = new BodyBuilder
        {
            HtmlBody = BuildOrderCancelledHtml(customerName, orderRef, note)
        };

        message.Body = builder.ToMessageBody();

        using var smtp = new SmtpClient();
        await smtp.ConnectAsync(_settings.SmtpHost, _settings.SmtpPort, SecureSocketOptions.StartTls);
        await smtp.AuthenticateAsync(_settings.From, _settings.Password);
        await smtp.SendAsync(message);
        await smtp.DisconnectAsync(true);

        logger.LogInformation("Order cancelled email sent for order {OrderId} to {CustomerEmail}.", orderId, customerEmail);
    }

    private static string BuildOrderCancelledHtml(string customerName, string orderRef, string? note)
    {
        var safeNote = string.IsNullOrWhiteSpace(note) ? null : System.Net.WebUtility.HtmlEncode(note).Replace("\n", "<br>");
        var noteBox = safeNote == null
            ? ""
            : $$"""
                <div style="margin-top: 16px; padding: 20px 24px; background: #f7ede6; border-radius: 18px;">
                  <div style="font-size: 12px; text-transform: uppercase; letter-spacing: 1px; color: #8b4513; margin-bottom: 8px;">Бележка от нас</div>
                  <div style="font-size: 15px; line-height: 1.7; color: #2c1810;">{{safeNote}}</div>
                </div>
                """;

        return $$"""
            <!DOCTYPE html>
            <html lang="bg">
            <head>
              <meta charset="utf-8">
              <title>Поръчката е отказана</title>
            </head>
            <body style="margin: 0; padding: 24px; background: #fdf8f4; font-family: Lato, Arial, sans-serif; color: #2c1810;">
              <div style="max-width: 640px; margin: 0 auto; background: #ffffff; border-radius: 24px; overflow: hidden; box-shadow: 0 16px 40px rgba(44, 24, 16, 0.12);">
                <div style="padding: 32px; background: linear-gradient(135deg, #2c1810 0%, #c97d4e 100%); color: #fff8f3;">
                  <div style="font-size: 13px; letter-spacing: 2px; text-transform: uppercase; opacity: 0.85;">Memory Atelier</div>
                  <h1 style="margin: 12px 0 8px; font-family: 'Playfair Display', Georgia, serif; font-size: 28px;">Поръчката е отказана</h1>
                  <p style="margin: 0; font-size: 15px; line-height: 1.6;">Здравейте, {{customerName}}! За съжаление поръчка №{{orderRef}} беше отказана.</p>
                </div>

                <div style="padding: 32px;">
                  <p style="font-size: 14px; color: #5e4639; line-height: 1.7;">
                    Ако сте платили по банков път, сумата ще ви бъде възстановена. При въпроси, не се колебайте да се свържете с нас.
                  </p>
                  {{noteBox}}
                </div>
              </div>
            </body>
            </html>
            """;
    }

    public async Task SendFinalShipmentSentAsync(string customerName, string customerEmail, Guid orderId, string productName, int quantity, bool isSplitRemainder)
    {
        var orderRef = orderId.ToString()[..8].ToUpperInvariant();
        var subjectPrefix = isSplitRemainder ? "Втората пратка на" : "Пратката на";

        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(_settings.From));
        message.To.Add(MailboxAddress.Parse(customerEmail));
        message.Subject = $"{subjectPrefix} поръчка №{orderRef} е изпратена!";

        var builder = new BodyBuilder
        {
            HtmlBody = BuildFinalShipmentSentHtml(customerName, orderRef, productName, quantity, isSplitRemainder)
        };

        message.Body = builder.ToMessageBody();

        using var smtp = new SmtpClient();
        await smtp.ConnectAsync(_settings.SmtpHost, _settings.SmtpPort, SecureSocketOptions.StartTls);
        await smtp.AuthenticateAsync(_settings.From, _settings.Password);
        await smtp.SendAsync(message);
        await smtp.DisconnectAsync(true);

        logger.LogInformation("Final shipment sent email sent for order {OrderId} to {CustomerEmail}.", orderId, customerEmail);
    }

    private static string BuildFinalShipmentSentHtml(string customerName, string orderRef, string productName, int quantity, bool isSplitRemainder)
    {
        var heading = isSplitRemainder ? "📦 Втората пратка е изпратена!" : "📦 Пратката е изпратена!";
        var intro = isSplitRemainder
            ? $"Останалата част от поръчка №{orderRef} вече е на път към вас."
            : $"Поръчка №{orderRef} вече е на път към вас.";

        return $$"""
            <!DOCTYPE html>
            <html lang="bg">
            <head>
              <meta charset="utf-8">
              <title>Пратката е изпратена</title>
            </head>
            <body style="margin: 0; padding: 24px; background: #fdf8f4; font-family: Lato, Arial, sans-serif; color: #2c1810;">
              <div style="max-width: 640px; margin: 0 auto; background: #ffffff; border-radius: 24px; overflow: hidden; box-shadow: 0 16px 40px rgba(44, 24, 16, 0.12);">
                <div style="padding: 32px; background: linear-gradient(135deg, #2c1810 0%, #c97d4e 100%); color: #fff8f3;">
                  <div style="font-size: 13px; letter-spacing: 2px; text-transform: uppercase; opacity: 0.85;">Memory Atelier</div>
                  <h1 style="margin: 12px 0 8px; font-family: 'Playfair Display', Georgia, serif; font-size: 28px;">{{heading}}</h1>
                  <p style="margin: 0; font-size: 15px; line-height: 1.6;">Здравейте, {{customerName}}! {{intro}}</p>
                </div>

                <div style="padding: 32px;">
                  <div style="padding: 20px 24px; background: #f7ede6; border-radius: 18px;">
                    <div style="font-size: 12px; text-transform: uppercase; letter-spacing: 1px; color: #8b4513; margin-bottom: 8px;">Продукт</div>
                    <div style="font-size: 15px; font-weight: 700; margin-bottom: 4px;">{{productName}}</div>
                    <div style="font-size: 15px; color: #5e4639;">{{quantity}} бр.</div>
                  </div>

                  <p style="margin-top: 24px; font-size: 14px; color: #5e4639; line-height: 1.7;">
                    С това поръчката ви е напълно изпратена. Благодарим ви, че пазарувате от Memory Atelier!
                  </p>
                </div>
              </div>
            </body>
            </html>
            """;
    }

    public async Task SendShipmentTimelineUpdateAsync(
        string customerName,
        string customerEmail,
        Guid orderId,
        string productName,
        string fulfillmentChoice,
        int quantity,
        int? shippedNowQuantity,
        DateTime? firstShipmentSentAt,
        DateTime? nextShipmentEstimatedAt)
    {
        var orderRef = orderId.ToString()[..8].ToUpperInvariant();

        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(_settings.From));
        message.To.Add(MailboxAddress.Parse(customerEmail));
        message.Subject = $"Актуализация за пратката на поръчка №{orderRef}";

        var builder = new BodyBuilder
        {
            HtmlBody = BuildShipmentTimelineUpdateHtml(customerName, orderRef, productName, fulfillmentChoice, quantity, shippedNowQuantity, firstShipmentSentAt, nextShipmentEstimatedAt)
        };

        message.Body = builder.ToMessageBody();

        using var smtp = new SmtpClient();
        await smtp.ConnectAsync(_settings.SmtpHost, _settings.SmtpPort, SecureSocketOptions.StartTls);
        await smtp.AuthenticateAsync(_settings.From, _settings.Password);
        await smtp.SendAsync(message);
        await smtp.DisconnectAsync(true);

        logger.LogInformation("Shipment timeline update email sent for order {OrderId} to {CustomerEmail}.", orderId, customerEmail);
    }

    private static string BuildShipmentTimelineUpdateHtml(
        string customerName,
        string orderRef,
        string productName,
        string fulfillmentChoice,
        int quantity,
        int? shippedNowQuantity,
        DateTime? firstShipmentSentAt,
        DateTime? nextShipmentEstimatedAt)
    {
        var culture = new CultureInfo("bg-BG");
        string FormatDate(DateTime? date) => date.HasValue ? date.Value.ToString("d MMMM yyyy", culture) : "";
        string FormatEstimate(DateTime? date) => date.HasValue ? $"Очаквана до {FormatDate(date)}" : "Предстои да бъде уточнена";

        var detailsBox = fulfillmentChoice == "Split"
            ? $$"""
                <div style="padding: 20px 24px; background: #f7ede6; border-radius: 18px;">
                  <div style="font-size: 12px; text-transform: uppercase; letter-spacing: 1px; color: #8b4513; margin-bottom: 8px;">Първа пратка ({{shippedNowQuantity ?? 0}} бр.)</div>
                  <div style="font-size: 15px; font-weight: 700; margin-bottom: 16px;">{{(firstShipmentSentAt.HasValue ? $"Изпратена на {FormatDate(firstShipmentSentAt)}" : "Все още не е изпратена")}}</div>
                  <div style="font-size: 12px; text-transform: uppercase; letter-spacing: 1px; color: #8b4513; margin-bottom: 8px;">Втора пратка ({{quantity - (shippedNowQuantity ?? 0)}} бр.)</div>
                  <div style="font-size: 15px; font-weight: 700;">{{FormatEstimate(nextShipmentEstimatedAt)}}</div>
                </div>
                """
            : $$"""
                <div style="padding: 20px 24px; background: #f7ede6; border-radius: 18px;">
                  <div style="font-size: 12px; text-transform: uppercase; letter-spacing: 1px; color: #8b4513; margin-bottom: 8px;">Очаквана дата на изпращане</div>
                  <div style="font-size: 15px; font-weight: 700;">{{(nextShipmentEstimatedAt.HasValue ? FormatDate(nextShipmentEstimatedAt) : "Предстои да бъде уточнена")}}</div>
                </div>
                """;

        return $$"""
            <!DOCTYPE html>
            <html lang="bg">
            <head>
              <meta charset="utf-8">
              <title>Актуализация за пратката</title>
            </head>
            <body style="margin: 0; padding: 24px; background: #fdf8f4; font-family: Lato, Arial, sans-serif; color: #2c1810;">
              <div style="max-width: 640px; margin: 0 auto; background: #ffffff; border-radius: 24px; overflow: hidden; box-shadow: 0 16px 40px rgba(44, 24, 16, 0.12);">
                <div style="padding: 32px; background: linear-gradient(135deg, #2c1810 0%, #c97d4e 100%); color: #fff8f3;">
                  <div style="font-size: 13px; letter-spacing: 2px; text-transform: uppercase; opacity: 0.85;">Memory Atelier</div>
                  <h1 style="margin: 12px 0 8px; font-family: 'Playfair Display', Georgia, serif; font-size: 28px;">Актуализация за вашата поръчка</h1>
                  <p style="margin: 0; font-size: 15px; line-height: 1.6;">Здравейте, {{customerName}}! Имаме новини за поръчка №{{orderRef}} — продукт „{{productName}}“.</p>
                </div>

                <div style="padding: 32px;">
                  {{detailsBox}}
                </div>
              </div>
            </body>
            </html>
            """;
    }

    private static string FulfillmentNoteHtml(OrderItemInfo item)
    {
        if (item.FulfillmentChoice == "Split")
        {
            var shippedNow = Math.Clamp(item.ShippedNowQuantity ?? 0, 0, item.Quantity);
            var remaining = item.Quantity - shippedNow;
            return $$"""<div style="margin-top: 6px; font-size: 12px; font-weight: 600; color: #c97d4e;">⚠ Разделена доставка: {{shippedNow}} бр. се изпращат сега, {{remaining}} бр. — с втора пратка.</div>""";
        }

        if (item.FulfillmentChoice == "Wait")
        {
            return """<div style="margin-top: 6px; font-size: 12px; font-weight: 600; color: #c97d4e;">⏳ Изчаква се пълното количество, за да се изпрати наведнъж. Ще получите отделен имейл с допълнителна информация за срока на изпращане.</div>""";
        }

        return "";
    }

    // culture (bg-BG) е за форматиране на дати; валутата винаги е евро, независимо от паричната единица на локала
    private static string FormatMoney(decimal value) => $"€{value.ToString("0.00", CultureInfo.InvariantCulture)}";

    private static string BuildItemRows(List<OrderItemInfo> items, CultureInfo culture)
    {
        var rows = new StringBuilder();

        foreach (var item in items)
        {
            rows.AppendLine($$"""
                <tr>
                  <td style="padding: 14px 16px; border-bottom: 1px solid #f1dfd3; color: #2c1810;">{{item.ProductName}}{{FulfillmentNoteHtml(item)}}</td>
                  <td style="padding: 14px 16px; border-bottom: 1px solid #f1dfd3; color: #2c1810; text-align: center;">{{item.Quantity}}</td>
                  <td style="padding: 14px 16px; border-bottom: 1px solid #f1dfd3; color: #2c1810; text-align: right;">{{FormatMoney(item.Price)}}</td>
                  <td style="padding: 14px 16px; border-bottom: 1px solid #f1dfd3; color: #2c1810; text-align: right; font-weight: 700;">{{FormatMoney(item.Price * item.Quantity)}}</td>
                </tr>
                """);
        }

        return rows.ToString();
    }

    private static string BuildBackorderBanner(List<OrderItemInfo> items)
    {
        if (!items.Any(item => !string.IsNullOrWhiteSpace(item.FulfillmentChoice)))
        {
            return "";
        }

        return """
            <div style="margin-top: 16px; padding: 14px 18px; background: rgba(255,255,255,0.16); border-radius: 14px; font-size: 13px; line-height: 1.6;">
              ⚠ Част от продуктите в тази поръчка надвишават текущата наличност — вижте забележките при съответните продукти по-долу.
            </div>
            """;
    }

    private string BuildOrderConfirmationHtml(
        string customerName,
        Guid orderId,
        string deliveryAddress,
        string deliveryMethod,
        string paymentMethod,
        List<OrderItemInfo> items,
        decimal total)
    {
        var culture = new CultureInfo("bg-BG");
        var orderRef = orderId.ToString()[..8].ToUpperInvariant();
        var deliveryMethodLabel = deliveryMethod == "SpeedyOffice" ? "До офис на Speedy" : "До адрес";
        var rows = BuildItemRows(items, culture);
        var introText = paymentMethod == "BankTransfer"
            ? $"За да обработим поръчка №{orderRef}, моля преведете сумата по-долу по банков път."
            : $"Поръчка №{orderRef} е приета и е в процес на подготовка.";
        var paymentSection = paymentMethod == "BankTransfer"
            ? BuildBankTransferSection(orderRef)
            : $$"""
                <p style="margin-top: 24px; font-size: 14px; color: #5e4639; line-height: 1.7;">
                  Ще заплатите сумата в брой на куриера при получаване на пратката. Разходите за доставка са за ваша сметка и се заплащат отделно, директно на куриера.
                </p>
                """;
        var backorderBanner = BuildBackorderBanner(items);

        return $$"""
            <!DOCTYPE html>
            <html lang="bg">
            <head>
              <meta charset="utf-8">
              <title>Потвърждение на поръчка</title>
            </head>
            <body style="margin: 0; padding: 24px; background: #fdf8f4; font-family: Lato, Arial, sans-serif; color: #2c1810;">
              <div style="max-width: 760px; margin: 0 auto; background: #ffffff; border-radius: 24px; overflow: hidden; box-shadow: 0 16px 40px rgba(44, 24, 16, 0.12);">
                <div style="padding: 32px; background: linear-gradient(135deg, #2c1810 0%, #c97d4e 100%); color: #fff8f3;">
                  <div style="font-size: 13px; letter-spacing: 2px; text-transform: uppercase; opacity: 0.85;">Memory Atelier</div>
                  <h1 style="margin: 12px 0 8px; font-family: 'Playfair Display', Georgia, serif; font-size: 32px;">Благодарим за поръчката!</h1>
                  <p style="margin: 0; font-size: 15px; line-height: 1.6;">Здравейте, {{customerName}}! {{introText}}</p>
                  {{backorderBanner}}
                </div>

                <div style="padding: 32px;">
                  <div style="margin-bottom: 24px; padding: 20px; background: #f7ede6; border-radius: 18px;">
                    <div style="font-size: 12px; text-transform: uppercase; letter-spacing: 1px; color: #8b4513; margin-bottom: 8px;">Данни за доставка</div>
                    <div style="font-size: 15px; font-weight: 700; margin-bottom: 4px;">{{deliveryMethodLabel}}</div>
                    <div style="font-size: 15px; color: #5e4639;">{{deliveryAddress}}</div>
                  </div>

                  <table style="width: 100%; border-collapse: collapse; border-radius: 18px; overflow: hidden; border: 1px solid #f1dfd3;">
                    <thead>
                      <tr style="background: #f5c4a1;">
                        <th style="padding: 14px 16px; text-align: left; color: #2c1810;">Продукт</th>
                        <th style="padding: 14px 16px; text-align: center; color: #2c1810;">Количество</th>
                        <th style="padding: 14px 16px; text-align: right; color: #2c1810;">Ед. цена</th>
                        <th style="padding: 14px 16px; text-align: right; color: #2c1810;">Сума</th>
                      </tr>
                    </thead>
                    <tbody>
                      {{rows}}
                    </tbody>
                  </table>

                  <div style="margin-top: 24px; padding: 20px 24px; background: #2c1810; border-radius: 18px; color: #fff8f3; text-align: right;">
                    <div style="font-size: 13px; text-transform: uppercase; letter-spacing: 1px; opacity: 0.8;">Обща стойност</div>
                    <div style="margin-top: 8px; font-size: 28px; font-weight: 700; font-family: 'Playfair Display', Georgia, serif;">{{FormatMoney(total)}}</div>
                  </div>

                  {{paymentSection}}
                </div>
              </div>
            </body>
            </html>
            """;
    }

    private string BuildBankTransferSection(string orderRef)
    {
        var bankNameRow = string.IsNullOrWhiteSpace(_bank.BankName)
            ? ""
            : $$"""
                <div style="margin-bottom: 10px;">
                  <span style="display: block; font-size: 12px; text-transform: uppercase; letter-spacing: 1px; color: #8b4513;">Банка</span>
                  <span style="font-size: 16px; font-weight: 600;">{{_bank.BankName}}</span>
                </div>
                """;

        return $$"""
            <div style="margin-top: 24px; padding: 20px 24px; background: #f7ede6; border-radius: 18px;">
              <div style="font-size: 12px; text-transform: uppercase; letter-spacing: 1px; color: #8b4513; margin-bottom: 8px;">Данни за банков превод</div>
              {{bankNameRow}}
              <div style="margin-bottom: 10px;">
                <span style="display: block; font-size: 12px; text-transform: uppercase; letter-spacing: 1px; color: #8b4513;">IBAN</span>
                <span style="font-size: 18px; font-weight: 700; letter-spacing: 0.5px;">{{_bank.Iban}}</span>
              </div>
              <div style="margin-bottom: 10px;">
                <span style="display: block; font-size: 12px; text-transform: uppercase; letter-spacing: 1px; color: #8b4513;">Титуляр на сметката</span>
                <span style="font-size: 16px; font-weight: 600;">{{_bank.AccountHolder}}</span>
              </div>
              <div>
                <span style="display: block; font-size: 12px; text-transform: uppercase; letter-spacing: 1px; color: #8b4513;">Основание за превод</span>
                <span style="font-size: 16px; font-weight: 600;">Поръчка №{{orderRef}}</span>
              </div>
            </div>

            <p style="margin-top: 24px; font-size: 14px; color: #5e4639; line-height: 1.7;">
              Моля, посочете номера на поръчката като основание за превода, за да можем бързо да я обработим.
              Ще изпратим поръчката веднага след като преводът бъде потвърден.
            </p>
            """;
    }

    private static string BuildOrderHtml(
        string customerName,
        string customerEmail,
        string deliveryPhone,
        string deliveryAddress,
        string deliveryMethod,
        string paymentMethod,
        List<OrderItemInfo> items,
        decimal total)
    {
        var culture = new CultureInfo("bg-BG");
        var deliveryMethodLabel = deliveryMethod == "SpeedyOffice" ? "До офис на Speedy" : "До адрес";
        var paymentMethodLabel = paymentMethod == "BankTransfer" ? "По банков път" : "Наложен платеж";
        var rows = BuildItemRows(items, culture);
        var backorderBanner = BuildBackorderBanner(items);

        return $$"""
            <!DOCTYPE html>
            <html lang="bg">
            <head>
              <meta charset="utf-8">
              <title>Нова поръчка</title>
            </head>
            <body style="margin: 0; padding: 24px; background: #fdf8f4; font-family: Lato, Arial, sans-serif; color: #2c1810;">
              <div style="max-width: 760px; margin: 0 auto; background: #ffffff; border-radius: 24px; overflow: hidden; box-shadow: 0 16px 40px rgba(44, 24, 16, 0.12);">
                <div style="padding: 32px; background: linear-gradient(135deg, #2c1810 0%, #c97d4e 100%); color: #fff8f3;">
                  <div style="font-size: 13px; letter-spacing: 2px; text-transform: uppercase; opacity: 0.85;">Memory Atelier</div>
                  <h1 style="margin: 12px 0 8px; font-family: 'Playfair Display', Georgia, serif; font-size: 32px;">Нова поръчка</h1>
                  <p style="margin: 0; font-size: 15px; line-height: 1.6;">Получихте нова заявка от клиента и можете да я прегледате по-долу.</p>
                  {{backorderBanner}}
                </div>

                <div style="padding: 32px;">
                  <div style="margin-bottom: 24px; padding: 20px; background: #f7ede6; border-radius: 18px;">
                    <div style="font-size: 12px; text-transform: uppercase; letter-spacing: 1px; color: #8b4513; margin-bottom: 8px;">Данни за клиента</div>
                    <div style="font-size: 18px; font-weight: 700; margin-bottom: 4px;">{{customerName}}</div>
                    <div style="font-size: 15px; color: #5e4639;">{{customerEmail}}</div>
                    <div style="font-size: 15px; color: #5e4639;">{{deliveryPhone}}</div>
                  </div>

                  <div style="margin-bottom: 24px; padding: 20px; background: #f7ede6; border-radius: 18px;">
                    <div style="font-size: 12px; text-transform: uppercase; letter-spacing: 1px; color: #8b4513; margin-bottom: 8px;">Данни за доставка</div>
                    <div style="font-size: 15px; font-weight: 700; margin-bottom: 4px;">{{deliveryMethodLabel}}</div>
                    <div style="font-size: 15px; color: #5e4639; margin-bottom: 10px;">{{deliveryAddress}}</div>
                    <div style="font-size: 13px; text-transform: uppercase; letter-spacing: 1px; color: #8b4513;">Начин на плащане</div>
                    <div style="font-size: 15px; color: #5e4639;">{{paymentMethodLabel}}</div>
                  </div>

                  <table style="width: 100%; border-collapse: collapse; border-radius: 18px; overflow: hidden; border: 1px solid #f1dfd3;">
                    <thead>
                      <tr style="background: #f5c4a1;">
                        <th style="padding: 14px 16px; text-align: left; color: #2c1810;">Продукт</th>
                        <th style="padding: 14px 16px; text-align: center; color: #2c1810;">Количество</th>
                        <th style="padding: 14px 16px; text-align: right; color: #2c1810;">Ед. цена</th>
                        <th style="padding: 14px 16px; text-align: right; color: #2c1810;">Сума</th>
                      </tr>
                    </thead>
                    <tbody>
                      {{rows}}
                    </tbody>
                  </table>

                  <div style="margin-top: 24px; padding: 20px 24px; background: #2c1810; border-radius: 18px; color: #fff8f3; text-align: right;">
                    <div style="font-size: 13px; text-transform: uppercase; letter-spacing: 1px; opacity: 0.8;">Обща стойност</div>
                    <div style="margin-top: 8px; font-size: 28px; font-weight: 700; font-family: 'Playfair Display', Georgia, serif;">{{FormatMoney(total)}}</div>
                  </div>
                </div>
              </div>
            </body>
            </html>
            """;
    }
}
