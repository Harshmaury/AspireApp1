FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/Services/Examination/Examination.API/Examination.API.csproj",                         "Services/Examination/Examination.API/"]
COPY ["src/Services/Examination/Examination.Application/Examination.Application.csproj",         "Services/Examination/Examination.Application/"]
COPY ["src/Services/Examination/Examination.Domain/Examination.Domain.csproj",                   "Services/Examination/Examination.Domain/"]
COPY ["src/Services/Examination/Examination.Infrastructure/Examination.Infrastructure.csproj",   "Services/Examination/Examination.Infrastructure/"]
COPY ["src/ServiceDefaults/AspireApp1.ServiceDefaults.csproj",                                   "ServiceDefaults/"]
COPY ["src/Shared/UMS.SharedKernel/UMS.SharedKernel.csproj",                                    "Shared/UMS.SharedKernel/"]
RUN dotnet restore "Services/Examination/Examination.API/Examination.API.csproj"
COPY src/ .
WORKDIR "/src/Services/Examination/Examination.API"
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
HEALTHCHECK --interval=30s --timeout=10s --retries=3 \
  CMD wget -qO- http://localhost:8080/health || exit 1
ENTRYPOINT ["dotnet", "Examination.API.dll"]
