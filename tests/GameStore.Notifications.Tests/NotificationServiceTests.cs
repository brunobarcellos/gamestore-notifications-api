using FluentAssertions;
using GameStore.Contracts.Events;
using GameStore.Notifications.Application.Services;
using GameStore.Notifications.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace GameStore.Notifications.Tests;

public class NotificationServiceTests
{
    private readonly Mock<INotificationRepository> _notificationRepositoryMock = new();

    private NotificationService CreateSut()
    {
        return new NotificationService(
            _notificationRepositoryMock.Object,
            NullLogger<NotificationService>.Instance
        );
    }

    [Fact]
    public async Task SendWelcomeEmailAsync_WhenUserCreatedEventReceived_ShouldPersistAndSimulateWelcomeEmail()
    {
        // Arrange
        var evt = new UserCreatedEvent(
            UserId: Guid.NewGuid(),
            Name: "Bruno Barcellos",
            Email: "bruno@gamestore.com",
            CreatedAt: DateTime.UtcNow
        );

        var sut = CreateSut();

        // Act
        var response = await sut.SendWelcomeEmailAsync(evt);

        // Assert
        response.Should().NotBeNull();
        response.RecipientEmail.Should().Be("bruno@gamestore.com");
        response.NotificationType.Should().Be(NotificationType.WelcomeEmail);
        response.Subject.Should().Contain("Bruno Barcellos");

        _notificationRepositoryMock.Verify(r => r.AddAsync(
            It.Is<EmailNotification>(n =>
                n.UserId == evt.UserId &&
                n.RecipientEmail == "bruno@gamestore.com" &&
                n.NotificationType == NotificationType.WelcomeEmail),
            It.IsAny<CancellationToken>()), Times.Once);
        _notificationRepositoryMock.Verify(r => r.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendPurchaseConfirmationEmailAsync_WhenPaymentApproved_ShouldSendConfirmationEmail()
    {
        // Arrange
        var evt = new PaymentProcessedEvent(
            PaymentId: Guid.NewGuid(),
            OrderId: Guid.NewGuid(),
            UserId: Guid.NewGuid(),
            GameId: Guid.NewGuid(),
            Price: 169.90m,
            Status: "Approved",
            Currency: "BRL",
            UserEmail: "gamer@gamestore.com",
            GameTitle: "Cyber Realm 2077"
        );

        var sut = CreateSut();

        // Act
        var response = await sut.SendPurchaseConfirmationEmailAsync(evt);

        // Assert
        response.Should().NotBeNull();
        response!.NotificationType.Should().Be(NotificationType.PurchaseConfirmationEmail);
        response.RecipientEmail.Should().Be("gamer@gamestore.com");
        response.Subject.Should().Contain("Cyber Realm 2077");

        _notificationRepositoryMock.Verify(r => r.AddAsync(It.IsAny<EmailNotification>(), It.IsAny<CancellationToken>()), Times.Once);
        _notificationRepositoryMock.Verify(r => r.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendPurchaseConfirmationEmailAsync_WhenPaymentRejected_ShouldNotSendEmail()
    {
        // Arrange
        var evt = new PaymentProcessedEvent(
            PaymentId: Guid.NewGuid(),
            OrderId: Guid.NewGuid(),
            UserId: Guid.NewGuid(),
            GameId: Guid.NewGuid(),
            Price: 169.90m,
            Status: "Rejected"
        );

        var sut = CreateSut();

        // Act
        var response = await sut.SendPurchaseConfirmationEmailAsync(evt);

        // Assert
        response.Should().BeNull();
        _notificationRepositoryMock.Verify(r => r.AddAsync(It.IsAny<EmailNotification>(), It.IsAny<CancellationToken>()), Times.Never);
        _notificationRepositoryMock.Verify(r => r.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}

