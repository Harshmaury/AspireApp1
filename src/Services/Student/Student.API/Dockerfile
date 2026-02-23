FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/Services/Student/Student.API/Student.API.csproj",                           "Services/Student/Student.API/"]
COPY ["src/Services/Student/Student.Application/Student.Application.csproj",           "Services/Student/Student.Application/"]
COPY ["src/Services/Student/Student.Domain/Student.Domain.csproj",                     "Services/Student/Student.Domain/"]
COPY ["src/Services/Student/Student.Infrastructure/Student.Infrastructure.csproj",     "Services/Student/Student.Infrastructure/"]
COPY ["src/ServiceDefaults/AspireApp1.ServiceDefaults.csproj",                          "ServiceDefaults/"]
COPY ["src/Shared/UMS.SharedKernel/UMS.SharedKernel.csproj",                           "Shared/UMS.SharedKernel/"]
RUN dotnet restore "Services/Student/Student.API/Student.API.csproj"
COPY src/ .
WORKDIR "/src/Services/Student/Student.API"
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
HEALTHCHECK --interval=30s --timeout=10s --retries=3 \
  CMD wget -qO- http://localhost:8080/health || exit 1
ENTRYPOINT ["dotnet", "Student.API.dll"]
