# ==============================================================================
# Stage 1: Build & Publish (Multi-Stage Build para Otimização em Produção)
# ==============================================================================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/GameStore.Notifications.Domain/GameStore.Notifications.Domain.csproj", "src/GameStore.Notifications.Domain/"]
COPY ["src/GameStore.Notifications.Application/GameStore.Notifications.Application.csproj", "src/GameStore.Notifications.Application/"]
COPY ["src/GameStore.Notifications.Infrastructure/GameStore.Notifications.Infrastructure.csproj", "src/GameStore.Notifications.Infrastructure/"]
COPY ["src/GameStore.Notifications.Api/GameStore.Notifications.Api.csproj", "src/GameStore.Notifications.Api/"]

RUN dotnet restore "src/GameStore.Notifications.Api/GameStore.Notifications.Api.csproj"

COPY src/ src/
WORKDIR "/src/src/GameStore.Notifications.Api"
RUN dotnet publish "GameStore.Notifications.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# ==============================================================================
# Stage 2: Runtime enxuto (ASP.NET Core Runtime)
# ==============================================================================
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

RUN mkdir -p /app/data && chown -R $APP_UID:$APP_UID /app/data

USER $APP_UID
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080
ENV ASPNETCORE_ENVIRONMENT=Production

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "GameStore.Notifications.Api.dll"]

