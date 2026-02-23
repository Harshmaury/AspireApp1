FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/Services/Notification/Notification.API/Notification.API.csproj",                         "Services/Notification/Notification.API/"]
COPY ["src/Services/Notification/Notification.Application/Notification.Application.csproj",         "Services/Notification/Notification.Application/"]
COPY ["src/Services/Notification/Notification.Domain/Notification.Domain.csproj",                   "Services/Notification/Notification.Domain/"]
COPY ["src/Services/Notification/Notification.Infrastructure/Notification.Infrastructure.csproj",   "Services/Notification/Notification.Infrastructure/"]
COPY ["src/ServiceDefaults/AspireApp1.ServiceDefaults.csproj",                                       "ServiceDefaults/"]
COPY ["src/Shared/UMS.SharedKernel/UMS.SharedKernel.csproj",                                        "Shared/UMS.SharedKernel/"]
RUN dotnet restore "Services/Notification/Notification.API/Notification.API.csproj"
COPY src/ .
WORKDIR "/src/Services/Notification/Notification.API"
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
HEALTHCHECK --interval=30s --timeout=10s --retries=3 \
  CMD wget -qO- http://localhost:8080/health || exit 1
ENTRYPOINT ["dotnet", "Notification.API.dll"]
