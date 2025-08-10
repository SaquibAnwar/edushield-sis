@echo off
echo 🚀 Starting EduShield SIS...

REM Check if Docker is running
docker info >nul 2>&1
if %errorlevel% neq 0 (
    echo ❌ Docker is not running. Please start Docker and try again.
    pause
    exit /b 1
)

REM Start PostgreSQL
echo 📦 Starting PostgreSQL database...
docker-compose up -d postgres

REM Wait for database to be ready
echo ⏳ Waiting for database to be ready...
timeout /t 10 /nobreak >nul

REM Navigate to API project
cd src\Api\EduShield.Api

REM Restore packages
echo 📦 Restoring NuGet packages...
dotnet restore

REM Build the project
echo 🔨 Building project...
dotnet build

REM Run database migrations
echo 🗄️ Running database migrations...
dotnet ef database update

REM Start the API
echo 🚀 Starting EduShield API...
echo 📍 API will be available at: https://localhost:7001
echo 📚 Swagger UI will be available at: https://localhost:7001/swagger
echo.
echo Press Ctrl+C to stop the application
echo.

dotnet run
