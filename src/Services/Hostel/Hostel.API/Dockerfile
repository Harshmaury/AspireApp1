FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/Services/Hostel/Hostel.API/Hostel.API.csproj",                         "Services/Hostel/Hostel.API/"]
COPY ["src/Services/Hostel/Hostel.Application/Hostel.Application.csproj",         "Services/Hostel/Hostel.Application/"]
COPY ["src/Services/Hostel/Hostel.Domain/Hostel.Domain.csproj",                   "Services/Hostel/Hostel.Domain/"]
COPY ["src/Services/Hostel/Hostel.Infrastructure/Hostel.Infrastructure.csproj",   "Services/Hostel/Hostel.Infrastructure/"]
COPY ["src/ServiceDefaults/AspireApp1.ServiceDefaults.csproj",                     "ServiceDefaults/"]
COPY ["src/Shared/UMS.SharedKernel/UMS.SharedKernel.csproj",                      "Shared/UMS.SharedKernel/"]
RUN dotnet restore "Services/Hostel/Hostel.API/Hostel.API.csproj"
COPY src/ .
WORKDIR "/src/Services/Hostel/Hostel.API"
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
HEALTHCHECK --interval=30s --timeout=10s --retries=3 \
  CMD wget -qO- http://localhost:8080/health || exit 1
ENTRYPOINT ["dotnet", "Hostel.API.dll"]
