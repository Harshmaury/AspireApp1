FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/Services/Attendance/Attendance.API/Attendance.API.csproj",                         "Services/Attendance/Attendance.API/"]
COPY ["src/Services/Attendance/Attendance.Application/Attendance.Application.csproj",         "Services/Attendance/Attendance.Application/"]
COPY ["src/Services/Attendance/Attendance.Domain/Attendance.Domain.csproj",                   "Services/Attendance/Attendance.Domain/"]
COPY ["src/Services/Attendance/Attendance.Infrastructure/Attendance.Infrastructure.csproj",   "Services/Attendance/Attendance.Infrastructure/"]
COPY ["src/ServiceDefaults/AspireApp1.ServiceDefaults.csproj",                                "ServiceDefaults/"]
COPY ["src/Shared/UMS.SharedKernel/UMS.SharedKernel.csproj",                                 "Shared/UMS.SharedKernel/"]
RUN dotnet restore "Services/Attendance/Attendance.API/Attendance.API.csproj"
COPY src/ .
WORKDIR "/src/Services/Attendance/Attendance.API"
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
HEALTHCHECK --interval=30s --timeout=10s --retries=3 \
  CMD wget -qO- http://localhost:8080/health || exit 1
ENTRYPOINT ["dotnet", "Attendance.API.dll"]
