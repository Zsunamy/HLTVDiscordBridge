# HLTVDiscordBridge Dockerfile with Memory Optimizations
# Multi-stage build to reduce final image size

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

# Install required packages for debugging and monitoring (optional)
RUN apk add --no-cache \
    curl \
    procps \
    htop

# Create a non-root user for security
RUN addgroup -g 1001 -S hltv && \
    adduser -S -D -H -u 1001 -h /app -s /sbin/nologin -G hltv -g hltv hltv

# Set working directory
WORKDIR /app

# Copy published application from build stage
COPY --from=build /app/publish .

# Create cache directory with proper permissions
# Note: config.xml will be mounted from host
RUN mkdir -p /app/cache/playercards /app/cache/teamcards /app/logs && \
    chown -R hltv:hltv /app && \
    chmod -R 755 /app

# Set memory optimization environment variables
ENV DOTNET_gcServer=1 \
    DOTNET_gcConcurrent=1 \
    DOTNET_GCLatencyLevel=1 \
    DOTNET_EnableWriteXorExecute=0 \
    DOTNET_ReadyToRun=1 \
    DOTNET_TieredPGO=1 \
    DOTNET_TC_QuickJitForLoops=1

# Set culture to avoid locale issues
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1

# Switch to non-root user
USER hltv

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
    CMD ps aux | grep -v grep | grep HLTVDiscordBridge || exit 1

# Expose port if needed (Discord bots typically don't need ports)
# EXPOSE 8080

# Set the entry point
ENTRYPOINT ["dotnet", "HLTVDiscordBridge.dll"]

# Add labels for metadata
LABEL maintainer="HLTVDiscordBridge Team" \
    description="HLTV Discord Bridge Bot with Memory Optimizations" \
    version="2.0-optimized"
