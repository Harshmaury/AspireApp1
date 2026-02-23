FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/Services/Faculty/Faculty.API/Faculty.API.csproj",                         "Services/Faculty/Faculty.API/"]
COPY ["src/Services/Faculty/Faculty.Application/Faculty.Application.csproj",         "Services/Faculty/Faculty.Application/"]
COPY ["src/Services/Faculty/Faculty.Domain/Faculty.Domain.csproj",                   "Services/Faculty/Faculty.Domain/"]
COPY ["src/Services/Faculty/Faculty.Infrastructure/Faculty.Infrastructure.csproj",   "Services/Faculty/Faculty.Infrastructure/"]
COPY ["src/ServiceDefaults/AspireApp1.ServiceDefaults.csproj",                        "ServiceDefaults/"]
COPY ["src/Shared/UMS.SharedKernel/UMS.SharedKernel.csproj",                         "Shared/UMS.SharedKernel/"]
RUN dotnet restore "Services/Faculty/Faculty.API/Faculty.API.csproj"
COPY src/ .
WORKDIR "/src/Services/Faculty/Faculty.API"
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
HEALTHCHECK --interval=30s --timeout=10s --retries=3 \
  CMD wget -qO- http://localhost:8080/health || exit 1
ENTRYPOINT ["dotnet", "Faculty.API.dll"]
