FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/Services/Academic/Academic.API/Academic.API.csproj",                         "Services/Academic/Academic.API/"]
COPY ["src/Services/Academic/Academic.Application/Academic.Application.csproj",         "Services/Academic/Academic.Application/"]
COPY ["src/Services/Academic/Academic.Domain/Academic.Domain.csproj",                   "Services/Academic/Academic.Domain/"]
COPY ["src/Services/Academic/Academic.Infrastructure/Academic.Infrastructure.csproj",   "Services/Academic/Academic.Infrastructure/"]
COPY ["src/ServiceDefaults/AspireApp1.ServiceDefaults.csproj",                          "ServiceDefaults/"]
COPY ["src/Shared/UMS.SharedKernel/UMS.SharedKernel.csproj",                           "Shared/UMS.SharedKernel/"]
RUN dotnet restore "Services/Academic/Academic.API/Academic.API.csproj"
COPY src/ .
WORKDIR "/src/Services/Academic/Academic.API"
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
HEALTHCHECK --interval=30s --timeout=10s --retries=3 \
  CMD wget -qO- http://localhost:8080/health || exit 1
ENTRYPOINT ["dotnet", "Academic.API.dll"]
