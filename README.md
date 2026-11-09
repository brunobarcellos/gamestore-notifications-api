# GameStore - NotificationsAPI

Microsserviço responsável por **simular o envio de e-mails transacionais**:
- **E-mail de Boas-Vindas**: consumido da fila `UserCreatedEvent`.
- **E-mail de Confirmação de Compra**: consumido da fila `PaymentProcessedEvent` (verifica se o status foi `Approved`).

## Executar Localmente (.NET 10)
```bash
dotnet run --project src/GameStore.Notifications.Api
```

## Build Docker (Multi-Stage)
```bash
docker build -t gamestore/notifications-api:latest .
```

## Deploy Kubernetes
```bash
kubectl apply -f k8s/
```

## Executar Testes
```bash
dotnet test NotificationsAPI.slnx
```

