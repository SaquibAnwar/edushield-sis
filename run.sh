#!/bin/bash

echo "🚀 Starting EduShield SIS..."

# Check if Docker is running
if ! docker info > /dev/null 2>&1; then
    echo "❌ Docker is not running. Please start Docker and try again."
    exit 1
fi

# Start PostgreSQL
echo "📦 Starting PostgreSQL database..."
docker-compose up -d postgres

# Wait for database to be ready
echo "⏳ Waiting for database to be ready..."
sleep 10

# Check database health
if docker-compose ps | grep -q "healthy"; then
    echo "✅ Database is ready!"
else
    echo "⚠️  Database might not be ready yet. Continuing anyway..."
fi

# Navigate to API project
cd src/Api/EduShield.Api

# Restore packages
echo "📦 Restoring NuGet packages..."
dotnet restore

# Build the project
echo "🔨 Building project..."
dotnet build

# Run database migrations
echo "🗄️  Running database migrations..."
dotnet ef database update

# Start the API
echo "🚀 Starting EduShield API..."
echo "📍 API will be available at: https://localhost:7001"
echo "📚 Swagger UI will be available at: https://localhost:7001/swagger"
echo ""
echo "Press Ctrl+C to stop the application"
echo ""

dotnet run
