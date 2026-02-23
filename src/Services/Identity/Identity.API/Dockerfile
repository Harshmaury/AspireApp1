FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/Services/Identity/Identity.API/Identity.API.csproj",                         "Services/Identity/Identity.API/"]
COPY ["src/Services/Identity/Identity.Application/Identity.Application.csproj",         "Services/Identity/Identity.Application/"]
COPY ["src/Services/Identity/Identity.Domain/Identity.Domain.csproj",                   "Services/Identity/Identity.Domain/"]
COPY ["src/Services/Identity/Identity.Infrastructure/Identity.Infrastructure.csproj",   "Services/Identity/Identity.Infrastructure/"]
COPY ["src/ServiceDefaults/AspireApp1.ServiceDefaults.csproj",                          "ServiceDefaults/"]
COPY ["src/Shared/UMS.SharedKernel/UMS.SharedKernel.csproj",                           "Shared/UMS.SharedKernel/"]
RUN dotnet restore "Services/Identity/Identity.API/Identity.API.csproj"
COPY src/ .
WORKDIR "/src/Services/Identity/Identity.API"
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
HEALTHCHECK --interval=30s --timeout=10s --retries=3 \
  CMD wget -qO- http://localhost:8080/health || exit 1
ENTRYPOINT ["dotnet", "Identity.API.dll"]
