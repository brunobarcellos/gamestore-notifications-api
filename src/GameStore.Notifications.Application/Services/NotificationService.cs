using GameStore.Contracts.Events;
using GameStore.Notifications.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GameStore.Notifications.Application.Services;

public record NotificationResponse(
    Guid Id,
    Guid UserId,
    string RecipientEmail,
    string NotificationType,
    string Subject,
    string Body,
    DateTime SentAt
);

public interface INotificationService
{
    Task<NotificationResponse> SendWelcomeEmailAsync(UserCreatedEvent userCreatedEvent, CancellationToken cancellationToken = default);
    Task<NotificationResponse?> SendPurchaseConfirmationEmailAsync(PaymentProcessedEvent paymentEvent, CancellationToken cancellationToken = default);
    Task<IEnumerable<NotificationResponse>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<NotificationResponse>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
}

public class NotificationService : INotificationService
{
    private readonly INotificationRepository _notificationRepository;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        INotificationRepository notificationRepository,
        ILogger<NotificationService> logger)
    {
        _notificationRepository = notificationRepository;
        _logger = logger;
    }

    public async Task<NotificationResponse> SendWelcomeEmailAsync(UserCreatedEvent userCreatedEvent, CancellationToken cancellationToken = default)
    {
        var subject = $"Bem-vindo(a) à GameStore, {userCreatedEvent.Name}!";
        var body = $"Olá {userCreatedEvent.Name}, sua conta ({userCreatedEvent.Email}) foi criada com sucesso em {userCreatedEvent.CreatedAt:dd/MM/yyyy HH:mm} UTC. Aproveite nosso catálogo de jogos!";

        var notification = new EmailNotification(
            userId: userCreatedEvent.UserId,
            recipientEmail: userCreatedEvent.Email,
            notificationType: NotificationType.WelcomeEmail,
            subject: subject,
            body: body
        );

        await _notificationRepository.AddAsync(notification, cancellationToken);
        await _notificationRepository.CommitAsync(cancellationToken);

        _logger.LogInformation(
            "[NotificationsAPI] [EMAIL SIMULADO - BOAS-VINDAS] Para: {Email} | Assunto: {Subject} | Mensagem: {Body}",
            notification.RecipientEmail, notification.Subject, notification.Body);

        return MapToResponse(notification);
    }

    public async Task<NotificationResponse?> SendPurchaseConfirmationEmailAsync(PaymentProcessedEvent paymentEvent, CancellationToken cancellationToken = default)
    {
        if (!string.Equals(paymentEvent.Status, "Approved", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning(
                "[NotificationsAPI] Pagamento {PaymentId} do pedido {OrderId} possui status '{Status}'. E-mail de confirmação de compra NÃO enviado.",
                paymentEvent.PaymentId, paymentEvent.OrderId, paymentEvent.Status);
            return null;
        }

        var recipient = string.IsNullOrWhiteSpace(paymentEvent.UserEmail)
            ? $"user-{paymentEvent.UserId}@gamestore.com"
            : paymentEvent.UserEmail;

        var gameLabel = string.IsNullOrWhiteSpace(paymentEvent.GameTitle)
            ? $"Jogo ID {paymentEvent.GameId}"
            : paymentEvent.GameTitle;

        var subject = $"Confirmação de Compra - {gameLabel}";
        var body = $"Seu pagamento ({paymentEvent.Currency} {paymentEvent.Price:N2}) para o jogo '{gameLabel}' (Pedido {paymentEvent.OrderId}) foi APROVADO! O jogo já está disponível em sua biblioteca.";

        var notification = new EmailNotification(
            userId: paymentEvent.UserId,
            recipientEmail: recipient,
            notificationType: NotificationType.PurchaseConfirmationEmail,
            subject: subject,
            body: body
        );

        await _notificationRepository.AddAsync(notification, cancellationToken);
        await _notificationRepository.CommitAsync(cancellationToken);

        _logger.LogInformation(
            "[NotificationsAPI] [EMAIL SIMULADO - CONFIRMAÇÃO DE COMPRA] Para: {Email} | Assunto: {Subject} | Mensagem: {Body}",
            notification.RecipientEmail, notification.Subject, notification.Body);

        return MapToResponse(notification);
    }

    public async Task<IEnumerable<NotificationResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var list = await _notificationRepository.GetAllAsync(cancellationToken);
        return list.Select(MapToResponse);
    }

    public async Task<IEnumerable<NotificationResponse>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var list = await _notificationRepository.GetByUserIdAsync(userId, cancellationToken);
        return list.Select(MapToResponse);
    }

    private static NotificationResponse MapToResponse(EmailNotification n) => new(
        n.Id,
        n.UserId,
        n.RecipientEmail,
        n.NotificationType,
        n.Subject,
        n.Body,
        n.SentAt
    );
}

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<INotificationService, NotificationService>();
        return services;
    }
}

