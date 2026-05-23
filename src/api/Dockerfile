# ═══════════════════════════════════════════════════════════════════════════════
# Logistics API — Multi-stage Dockerfile (.NET 8)
# ═══════════════════════════════════════════════════════════════════════════════

# ── Stage 1: Restore & Build ──────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project file and restore — separate layer so restores are cached
COPY src/api/*.csproj ./src/api/
RUN dotnet restore src/api/*.csproj

# Copy everything and build
COPY src/api/ ./src/api/
WORKDIR /src/src/api
RUN dotnet build -c Release -o /app/build

# ── Stage 2: Publish ──────────────────────────────────────────────────────────
FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# ── Stage 3: Runtime image ────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Non-root user for security
RUN addgroup --system appgroup && adduser --system --ingroup appgroup appuser
USER appuser

COPY --from=publish /app/publish .

# Health check endpoint (implement /health in your API)
HEALTHCHECK --interval=30s --timeout=10s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1

EXPOSE 8080
ENTRYPOINT ["dotnet", "LogisticsApi.dll"]
