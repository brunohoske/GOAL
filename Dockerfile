# ---- Build stage -------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first with only the csproj files so Docker caches the NuGet layer.
COPY src/Goal.Domain/Goal.Domain.csproj        src/Goal.Domain/
COPY src/Goal.Application/Goal.Application.csproj src/Goal.Application/
COPY src/Goal.Infrastructure/Goal.Infrastructure.csproj src/Goal.Infrastructure/
COPY src/Goal.Api/Goal.Api.csproj              src/Goal.Api/
RUN dotnet restore src/Goal.Api/Goal.Api.csproj

COPY src/ src/
RUN dotnet publish src/Goal.Api/Goal.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

# ---- Runtime stage ------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Uploads live here; mount a volume so they survive container recreation.
RUN mkdir -p /app/wwwroot/uploads

ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Goal.Api.dll"]
