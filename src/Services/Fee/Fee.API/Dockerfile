FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/Services/Fee/Fee.API/Fee.API.csproj",                       "Services/Fee/Fee.API/"]
COPY ["src/Services/Fee/Fee.Application/Fee.Application.csproj",       "Services/Fee/Fee.Application/"]
COPY ["src/Services/Fee/Fee.Domain/Fee.Domain.csproj",                 "Services/Fee/Fee.Domain/"]
COPY ["src/Services/Fee/Fee.Infrastructure/Fee.Infrastructure.csproj", "Services/Fee/Fee.Infrastructure/"]
COPY ["src/ServiceDefaults/AspireApp1.ServiceDefaults.csproj",          "ServiceDefaults/"]
COPY ["src/Shared/UMS.SharedKernel/UMS.SharedKernel.csproj",           "Shared/UMS.SharedKernel/"]
RUN dotnet restore "Services/Fee/Fee.API/Fee.API.csproj"
COPY src/ .
WORKDIR "/src/Services/Fee/Fee.API"
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
HEALTHCHECK --interval=30s --timeout=10s --retries=3 \
  CMD wget -qO- http://localhost:8080/health || exit 1
ENTRYPOINT ["dotnet", "Fee.API.dll"]
