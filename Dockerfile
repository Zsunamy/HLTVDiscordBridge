# Use the official .NET runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS base

# Install ICU libraries for globalization support
RUN apk add --no-cache icu-libs

# Set working directory
WORKDIR /app

# Use the SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
WORKDIR /src

# Copy project file and restore dependencies
COPY *.csproj ./
RUN dotnet restore

# Copy source code and build
COPY . .
RUN dotnet build -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Final stage
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Ensure the app runs as non-root user for security
RUN adduser -D -s /bin/sh appuser && chown -R appuser:appuser /app
USER appuser

ENTRYPOINT ["dotnet", "HLTVDiscordBridge.dll"]
