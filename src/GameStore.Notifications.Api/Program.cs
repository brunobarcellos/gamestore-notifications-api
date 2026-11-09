using GameStore.Notifications.Application.Services;
using GameStore.Notifications.Infrastructure;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.Services.AddHealthChecks();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddHttpClient("UserApi", client =>
{
    var url = builder.Configuration["Services:UserApiUrl"] ?? "http://user-api:80";
    client.BaseAddress = new Uri(url);
});
builder.Services.AddHttpClient("CatalogApi", client =>
{
    var url = builder.Configuration["Services:CatalogApiUrl"] ?? "http://catalog-api:80";
    client.BaseAddress = new Uri(url);
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "GameStore - NotificationsAPI",
        Version = "v1",
        Description = "Microsserviço de Notificações por E-mail (consome UserCreatedEvent e PaymentProcessedEvent)."
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "GameStore NotificationsAPI v1");
    c.RoutePrefix = "swagger";
    c.DocumentTitle = "GameStore NotificationsAPI - Swagger UI";
});

app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();
app.MapHealthChecks("/health").ExcludeFromDescription();

var group = app.MapGroup("/api/notifications").WithTags("1. Notificações e E-mails Enviados");

group.MapGet("/", async (INotificationService notificationService, CancellationToken ct) =>
{
    var notifications = await notificationService.GetAllAsync(ct);
    return Results.Ok(notifications);
})
.WithName("GetAllNotifications")
.WithSummary("Listar todos os e-mails simulados enviados (Boas-vindas e Confirmação de Compra)")
.Produces<IEnumerable<NotificationResponse>>(StatusCodes.Status200OK);

group.MapGet("/user/{userId:guid}", async (Guid userId, INotificationService notificationService, CancellationToken ct) =>
{
    var notifications = await notificationService.GetByUserIdAsync(userId, ct);
    return Results.Ok(notifications);
})
.WithName("GetNotificationsByUser")
.WithSummary("Listar e-mails enviados para um usuário específico")
.Produces<IEnumerable<NotificationResponse>>(StatusCodes.Status200OK);

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();
    await dbContext.Database.EnsureCreatedAsync();
}

app.Run();

public partial class Program { }

