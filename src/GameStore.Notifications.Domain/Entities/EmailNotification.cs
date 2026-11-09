namespace GameStore.Notifications.Domain.Entities;

public static class NotificationType
{
    public const string WelcomeEmail = "WelcomeEmail";
    public const string PurchaseConfirmationEmail = "PurchaseConfirmationEmail";
}

/// <summary>
/// Entidade que registra um e-mail simulado enviado pelo microsserviço NotificationsAPI.
/// </summary>
public class EmailNotification
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string RecipientEmail { get; private set; } = null!;
    public string NotificationType { get; private set; } = null!;
    public string Subject { get; private set; } = null!;
    public string Body { get; private set; } = null!;
    public DateTime SentAt { get; private set; }

    protected EmailNotification() { }

    public EmailNotification(
        Guid userId,
        string recipientEmail,
        string notificationType,
        string subject,
        string body)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        RecipientEmail = string.IsNullOrWhiteSpace(recipientEmail) ? $"user-{userId}@gamestore.com" : recipientEmail.Trim();
        NotificationType = notificationType;
        Subject = subject;
        Body = body;
        SentAt = DateTime.UtcNow;
    }
}

public interface INotificationRepository
{
    Task AddAsync(EmailNotification notification, CancellationToken cancellationToken = default);
    Task<IEnumerable<EmailNotification>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<EmailNotification>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<int> CommitAsync(CancellationToken cancellationToken = default);
}

