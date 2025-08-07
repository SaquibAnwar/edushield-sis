# EduShield SIS (Student Information System)

A modern Student Information System built with .NET 8 and ASP.NET Core Minimal APIs.

## Architecture

- **API Layer**: ASP.NET Core Minimal API (`src/Api/EduShield.Api`)
- **Tests**: xUnit test project (`tests/Api/EduShield.Api.Tests`)
- **Infrastructure**: Docker Compose setup for development dependencies

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker](https://www.docker.com/get-started)
- [Docker Compose](https://docs.docker.com/compose/install/)

## Getting Started

### 1. Clone the Repository

```bash
git clone <repository-url>
cd edushield-sis
```

### 2. Start the Development Stack

The project includes a Docker Compose configuration that provides all necessary development dependencies:

- **PostgreSQL 15**: Primary database (port 5432)
- **Redis 7**: Caching and session storage (port 6379)
- **MailHog**: Email testing tool (SMTP: 1025, Web UI: 8025)

```bash
# Start all services
docker compose -f docker-compose.test.yml up -d

# Check service status
docker compose -f docker-compose.test.yml ps

# View logs
docker compose -f docker-compose.test.yml logs
```

### 3. Build and Run the Application

```bash
# Restore dependencies
dotnet restore

# Build the solution
dotnet build

# Run the API
dotnet run --project src/Api/EduShield.Api/EduShield.Api.csproj
```

The API will be available at:
- HTTP: `http://localhost:5000`
- HTTPS: `https://localhost:5001`
- Swagger UI: `https://localhost:5001/swagger`

### 4. Run Tests

```bash
# Run all tests
dotnet test --verbosity normal

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run tests in watch mode
dotnet test --watch
```

## Developer Setup

### Enable Git Hooks

To ensure code quality and consistency, set up the automated pre-commit hooks:

```bash
# Configure Git to use the project's hooks directory
git config core.hooksPath .githooks
```

This enables automatic checks before each commit:
- **Code Formatting**: Runs `dotnet format --verify-no-changes`
- **Tests**: Runs `dotnet test --nologo`

The commit will be blocked if either check fails, ensuring consistent code quality across the team.

### Editor Configuration

The project includes an `.editorconfig` file that enforces:
- UTF-8 encoding
- 4-space indentation
- 120-character line limit
- Consistent .NET coding standards

Most modern editors (Visual Studio, VS Code, JetBrains Rider) automatically respect these settings.

## Development Services

### PostgreSQL Database
- **Host**: localhost
- **Port**: 5432
- **Database**: edushield_test
- **Username**: postgres
- **Password**: postgres

### Redis Cache
- **Host**: localhost
- **Port**: 6379

### MailHog (Email Testing)
- **SMTP Server**: localhost:1025
- **Web Interface**: http://localhost:8025

## Project Structure

```
edushield-sis/
├── src/
│   └── Api/
│       └── EduShield.Api/           # ASP.NET Core Minimal API
├── tests/
│   └── Api/
│       └── EduShield.Api.Tests/     # xUnit test project
├── .github/
│   └── workflows/
│       └── ci.yml                   # GitHub Actions CI pipeline
├── docker-compose.test.yml          # Development dependencies
├── EduShield.sln                    # Solution file
├── .gitignore                       # Git ignore rules
└── README.md                        # This file
```

## CI/CD

The project includes a GitHub Actions workflow (`.github/workflows/ci.yml`) that:

1. Checks out the code
2. Sets up .NET 8
3. Starts the Docker Compose test stack
4. Runs all tests
5. Tears down the test infrastructure

## Contributing

1. Create a feature branch from `main`
2. Make your changes
3. Add/update tests as needed
4. Ensure all tests pass: `dotnet test`
5. Submit a pull request

## Troubleshooting

### Docker Issues

```bash
# Stop all services
docker compose -f docker-compose.test.yml down

# Remove volumes (will delete data)
docker compose -f docker-compose.test.yml down -v

# Rebuild and restart
docker compose -f docker-compose.test.yml up -d --build
```

### Database Connection Issues

Ensure PostgreSQL is running and accessible:

```bash
# Check if PostgreSQL is responding
docker compose -f docker-compose.test.yml exec postgres pg_isready -U postgres

# Connect to database
docker compose -f docker-compose.test.yml exec postgres psql -U postgres -d edushield_test
```

### Test Issues

```bash
# Clean and rebuild
dotnet clean
dotnet build

# Run specific test project
dotnet test tests/Api/EduShield.Api.Tests/
```
