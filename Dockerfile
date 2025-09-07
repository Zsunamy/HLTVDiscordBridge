# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build

# Set working directory
WORKDIR /src

# Copy project file and restore dependencies
COPY HLTVDiscordBridge.csproj .
RUN dotnet restore --use-current-runtime

# Copy source code
COPY . .

# Build the application in Release mode with optimizations
RUN dotnet publish -c Release -o /app/publish \
    --use-current-runtime \
    --self-contained false \
    --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/runtime:9.0-alpine AS runtime

# Install required packages for debugging and monitoring
RUN apk add --no-cache \
    curl \
    procps \
    htop

# Set working directory
WORKDIR /app

# Copy published application from build stage
COPY --from=build /app/publish .

# Create cache and logs directories
RUN mkdir -p /app/cache/playercards /app/cache/teamcards /app/cache/events /app/logs && \
    chmod -R 755 /app

# Set memory optimization environment variables
ENV DOTNET_gcServer=1 \
    DOTNET_gcConcurrent=1 \
    DOTNET_GCLatencyLevel=1 \
    DOTNET_EnableWriteXorExecute=0 \
    DOTNET_ReadyToRun=1 \
    DOTNET_TieredPGO=1 \
    DOTNET_TC_QuickJitForLoops=1

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
    CMD ps aux | grep -v grep | grep HLTVDiscordBridge || exit 1

# Set the entry point
ENTRYPOINT ["dotnet", "HLTVDiscordBridge.dll"]

# Add labels for metadata
LABEL maintainer="HLTVDiscordBridge Team" \
    description="HLTV Discord Bridge Bot with Memory Optimizations" \
    version="2.0-optimized"
