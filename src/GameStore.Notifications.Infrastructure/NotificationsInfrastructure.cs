using GameStore.Contracts.Events;
using GameStore.Notifications.Application.Services;
using GameStore.Notifications.Domain.Entities;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GameStore.Notifications.Infrastructure;

public class NotificationsDbContext : DbContext
{
    public DbSet<EmailNotification> Notifications => Set<EmailNotification>();

    public NotificationsDbContext(DbContextOptions<NotificationsDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<EmailNotification>(builder =>
        {
            builder.ToTable("EmailNotifications");
            builder.HasKey(n => n.Id);
            builder.Property(n => n.UserId).IsRequired();
            builder.Property(n => n.RecipientEmail).HasMaxLength(254).IsRequired();
            builder.Property(n => n.NotificationType).HasMaxLength(60).IsRequired();
            builder.Property(n => n.Subject).HasMaxLength(250).IsRequired();
            builder.Property(n => n.Body).HasMaxLength(2000).IsRequired();
            builder.Property(n => n.SentAt).IsRequired();
        });
    }
}

public class NotificationRepository : INotificationRepository
{
    private readonly NotificationsDbContext _context;

    public NotificationRepository(NotificationsDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(EmailNotification notification, CancellationToken cancellationToken = default)
        => await _context.Notifications.AddAsync(notification, cancellationToken);

    public async Task<IEnumerable<EmailNotification>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _context.Notifications.OrderByDescending(n => n.SentAt).ToListAsync(cancellationToken);

    public async Task<IEnumerable<EmailNotification>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        => await _context.Notifications.Where(n => n.UserId == userId).OrderByDescending(n => n.SentAt).ToListAsync(cancellationToken);

    public async Task<int> CommitAsync(CancellationToken cancellationToken = default)
        => await _context.SaveChangesAsync(cancellationToken);
}

public class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";
    public string Host { get; set; } = "localhost";
    public ushort Port { get; set; } = 5672;
    public string VirtualHost { get; set; } = "/";
    public string Username { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public NotificationQueueOptions Queues { get; set; } = new();
}

public class NotificationQueueOptions
{
    public string UserCreated { get; set; } = "UserCreatedEvent";
    public string PaymentProcessedNotifications { get; set; } = "PaymentProcessedEvent.Notifications";
}

/// <summary>
/// Consumidor do evento <see cref="UserCreatedEvent"/> para envio de e-mail de boas-vindas.
/// </summary>
public class UserCreatedConsumer : IConsumer<UserCreatedEvent>
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<UserCreatedConsumer> _logger;

    public UserCreatedConsumer(
        INotificationService notificationService,
        ILogger<UserCreatedConsumer> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<UserCreatedEvent> context)
    {
        var message = context.Message;
        _logger.LogInformation(
            "[NotificationsAPI] Consumindo UserCreatedEvent: UserId={UserId}, Name={Name}, Email={Email}",
            message.UserId, message.Name, message.Email);

        await _notificationService.SendWelcomeEmailAsync(message, context.CancellationToken);
    }
}

/// <summary>
/// Consumidor do evento <see cref="PaymentProcessedEvent"/> para envio de confirmação de compra se Approved.
/// </summary>
public class PaymentProcessedNotificationConsumer : IConsumer<PaymentProcessedEvent>
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<PaymentProcessedNotificationConsumer> _logger;

    public PaymentProcessedNotificationConsumer(
        INotificationService notificationService,
        ILogger<PaymentProcessedNotificationConsumer> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<PaymentProcessedEvent> context)
    {
        var message = context.Message;
        _logger.LogInformation(
            "[NotificationsAPI] Consumindo PaymentProcessedEvent: PaymentId={PaymentId}, OrderId={OrderId}, Status={Status}",
            message.PaymentId, message.OrderId, message.Status);

        await _notificationService.SendPurchaseConfirmationEmailAsync(message, context.CancellationToken);
    }
}

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<RabbitMqOptions>(configuration.GetSection(RabbitMqOptions.SectionName));

        var connectionString = configuration.GetConnectionString("DefaultConnection") ?? "Data Source=notifications.db";
        services.AddDbContext<NotificationsDbContext>(options =>
            options.UseSqlite(connectionString));

        services.AddScoped<INotificationRepository, NotificationRepository>();

        var rabbitOptions = configuration.GetSection(RabbitMqOptions.SectionName).Get<RabbitMqOptions>() ?? new RabbitMqOptions();

        services.AddMassTransit(x =>
        {
            x.AddConsumer<UserCreatedConsumer>();
            x.AddConsumer<PaymentProcessedNotificationConsumer>();

            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(rabbitOptions.Host, rabbitOptions.Port, rabbitOptions.VirtualHost, h =>
                {
                    h.Username(rabbitOptions.Username);
                    h.Password(rabbitOptions.Password);
                });

                cfg.ReceiveEndpoint(rabbitOptions.Queues.UserCreated, e =>
                {
                    e.ConfigureConsumer<UserCreatedConsumer>(context);
                });

                cfg.ReceiveEndpoint(rabbitOptions.Queues.PaymentProcessedNotifications, e =>
                {
                    e.ConfigureConsumer<PaymentProcessedNotificationConsumer>(context);
                });
            });
        });

        return services;
    }
}

