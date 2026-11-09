namespace GameStore.Contracts.Events;

/// <summary>
/// Evento consumido pela NotificationsAPI para simular envio de e-mail de boas-vindas.
/// </summary>
public record UserCreatedEvent(
    Guid UserId,
    string Name,
    string Email,
    DateTime CreatedAt
);

/// <summary>
/// Evento consumido pela NotificationsAPI para simular envio de confirmação de compra quando Approved.
/// </summary>
public record PaymentProcessedEvent(
    Guid PaymentId,
    Guid OrderId,
    Guid UserId,
    Guid GameId,
    decimal Price,
    string Status,
    string Currency = "BRL",
    string? UserEmail = null,
    string? GameTitle = null,
    DateTime ProcessedAt = default
);

