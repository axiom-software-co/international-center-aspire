#!/bin/bash

# Development setup script for International Center Website
# Integrates Bun runtime with .NET Aspire orchestration

set -e

echo "🏥 International Center Website - Development Setup"
echo "================================================="

# Check if bun is installed
if ! command -v bun &> /dev/null; then
    echo "❌ Bun runtime not found!"
    echo "📥 Please install Bun from: https://bun.sh/"
    echo "   curl -fsSL https://bun.sh/install | bash"
    exit 1
fi

echo "✅ Bun runtime detected: $(bun --version)"

# Check if .NET 9 is available
if ! command -v dotnet &> /dev/null; then
    echo "❌ .NET runtime not found!"
    echo "📥 Please install .NET 9 SDK"
    exit 1
fi

echo "✅ .NET runtime detected: $(dotnet --version)"

# Install frontend dependencies
echo "📦 Installing frontend dependencies with Bun..."
bun install --frozen-lockfile

# Build frontend for development
echo "🏗️ Building frontend for development..."
bun run build

# Check if dist directory was created
if [ ! -d "dist" ]; then
    echo "❌ Frontend build failed - dist directory not found"
    exit 1
fi

echo "✅ Frontend build completed successfully"

# Build .NET project
echo "🔨 Building .NET Website project..."
dotnet build

echo "🎉 Development setup complete!"
echo ""
echo "🚀 To start development:"
echo "   1. Terminal 1: dotnet run (starts .NET host on port 5000/5001)"
echo "   2. Terminal 2: bun run dev (starts Astro dev server on port 4321)"
echo ""
echo "📊 The .NET host will proxy to Astro dev server for hot reload"
echo "🌐 Access the website at: http://localhost:5000 or https://localhost:5001"
echo "🔥 Frontend dev server: http://localhost:4321"
echo ""
echo "🧪 For testing:"
echo "   • Unit tests: bun run test:unit"
echo "   • Integration tests: bun run test:integration" 
echo "   • E2E tests: bun run test:e2e"
echo ""
echo "📋 For Aspire orchestration, run from app-host directory:"
echo "   dotnet run"