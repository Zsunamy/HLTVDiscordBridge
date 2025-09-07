#!/bin/bash

# HLTVDiscordBridge Docker Build Script with Memory Optimizations

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Configuration
IMAGE_NAME="hltv-discord-bridge"
TAG="${1:-latest}"
FULL_IMAGE_NAME="${IMAGE_NAME}:${TAG}"

echo -e "${GREEN}🐳 Building HLTVDiscordBridge Docker Image${NC}"
echo -e "${YELLOW}Image: ${FULL_IMAGE_NAME}${NC}"
echo ""

# Check if Docker is running
if ! docker info > /dev/null 2>&1; then
    echo -e "${RED}❌ Docker is not running. Please start Docker and try again.${NC}"
    exit 1
fi

# Create .env file if it doesn't exist
if [ ! -f .env ]; then
    echo -e "${YELLOW}⚠️  .env file not found. Creating from template...${NC}"
    cp .env.template .env
    echo -e "${YELLOW}📝 Please edit .env file with your configuration.${NC}"
fi

# Create config.xml if it doesn't exist
if [ ! -f config.xml ]; then
    echo -e "${YELLOW}⚠️  config.xml file not found. Creating from template...${NC}"
    cp config.xml.template config.xml
    echo -e "${YELLOW}📝 Please edit config.xml file with your bot configuration.${NC}"
fi

# Create directories for mounted volumes
mkdir -p cache logs
echo -e "${GREEN}📁 Created cache and logs directories${NC}"

# Build the Docker image
echo -e "${GREEN}📦 Building Docker image...${NC}"
docker build \
    --tag "${FULL_IMAGE_NAME}" \
    --build-arg BUILDKIT_INLINE_CACHE=1 \
    --progress=plain \
    .

if [ $? -eq 0 ]; then
    echo -e "${GREEN}✅ Docker image built successfully!${NC}"
    echo ""
    
    # Show image information
    echo -e "${GREEN}📊 Image Information:${NC}"
    docker images "${IMAGE_NAME}" --format "table {{.Repository}}\t{{.Tag}}\t{{.Size}}\t{{.CreatedAt}}"
    echo ""
    
    # Show memory optimization features
    echo -e "${GREEN}🎯 Memory Optimization Features:${NC}"
    echo "   ✅ Multi-stage build for minimal image size"
    echo "   ✅ Alpine Linux base for reduced overhead"
    echo "   ✅ .NET Server GC enabled"
    echo "   ✅ Concurrent GC enabled"
    echo "   ✅ Optimized GC latency mode"
    echo "   ✅ Runtime optimizations enabled"
    echo "   ✅ Memory limits in docker-compose.yml"
    echo ""
    
    echo -e "${GREEN}🚀 Next Steps:${NC}"
    echo "1. Edit config.xml with your bot configuration"
    echo "2. Edit .env file if needed (optional runtime settings)"
    echo "3. Run: docker-compose up -d"
    echo "4. Monitor: docker-compose logs -f hltv-discord-bridge"
    echo "5. Check memory: docker stats hltv-discord-bridge"
    echo ""
    
    # Optional: Run the container immediately
    read -p "Do you want to start the container now? (y/n): " -n 1 -r
    echo
    if [[ $REPLY =~ ^[Yy]$ ]]; then
        echo -e "${GREEN}🚀 Starting container...${NC}"
        docker-compose up -d
        
        echo -e "${GREEN}📋 Container started! Useful commands:${NC}"
        echo "   View logs: docker-compose logs -f"
        echo "   Check status: docker-compose ps"
        echo "   Stop: docker-compose down"
        echo "   Monitor memory: docker stats"
    fi
    
else
    echo -e "${RED}❌ Docker build failed!${NC}"
    exit 1
fi

# Clean up dangling images (optional)
read -p "Do you want to clean up dangling images? (y/n): " -n 1 -r
echo
if [[ $REPLY =~ ^[Yy]$ ]]; then
    echo -e "${GREEN}🧹 Cleaning up dangling images...${NC}"
    docker image prune -f
fi

echo -e "${GREEN}✨ Build process completed!${NC}"
