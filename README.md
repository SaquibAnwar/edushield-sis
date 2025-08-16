# EduShield SIS (Student Information System)

A modern .NET 8 Student Information System built with Entity Framework Core, PostgreSQL, and JWT authentication.

## Features

- Student management (CRUD operations)
- Faculty management
- Performance tracking
- Fee management
- JWT-based authentication
- Role-based authorization
- AutoMapper for object mapping
- FluentValidation for request validation
- Health checks
- Swagger/OpenAPI documentation

## Prerequisites

- .NET 8.0 SDK
- Docker and Docker Compose
- PostgreSQL (or use Docker)

## Quick Start

### 1. Setup OAuth Credentials (Required for Google Authentication)

**Quick Setup (Recommended):**
```bash
# Run the setup script (Linux/Mac)
./setup-oauth.sh

# Or on Windows
setup-oauth.bat
```

**Manual Setup:**
```bash
# Copy the secrets template
cp src/Api/EduShield.Api/appsettings.Secrets.template.json src/Api/EduShield.Api/appsettings.Secrets.json

# Edit the secrets file with your Google OAuth credentials
# Get these from Google Cloud Console > APIs & Services > Credentials
```

**Important**: The `appsettings.Secrets.json` file is gitignored and should never be committed to version control.

#### Google OAuth Setup:
1. Go to [Google Cloud Console](https://console.cloud.google.com/)
2. Create a new project or select existing one
3. Enable Google+ API
4. Go to "APIs & Services" > "Credentials"
5. Create OAuth 2.0 Client ID
6. Add `http://localhost:5000/api/v1/auth/callback/google` to authorized redirect URIs
7. Copy Client ID and Client Secret to your `appsettings.Secrets.json`

### 2. Start the Database

```bash
# Start PostgreSQL using Docker Compose
docker-compose up -d postgres

# Wait for the database to be ready (check health status)
docker-compose ps
```

### 3. Run Database Migrations

```bash
# Navigate to the API project
cd src/Api/EduShield.Api

# Create the database and run migrations
dotnet ef database update
```

### 4. Run the Application

```bash
# From the API project directory
dotnet run

# Or from the solution root
dotnet run --project src/Api/EduShield.Api
```

The API will be available at:
- **API**: http://localhost:5000
- **Swagger UI**: http://localhost:5000/swagger
- **Health Check**: http://localhost:5000/api/v1/health/live
- **Google OAuth Login**: http://localhost:5000/api/v1/auth/login/google

### 5. Test the API

The application uses development authentication in development mode, so you can test the endpoints without providing JWT tokens.

#### Create a Student
```bash
curl -X POST "https://localhost:7001/v1/students" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "John Doe",
    "class": "10",
    "section": "A",
    "gender": 0
  }'
```

#### Get All Students
```bash
curl "https://localhost:7001/v1/students"
```

## Project Structure

```
src/
├── Api/
│   └── EduShield.Api/          # Web API project
│       ├── Auth/               # Authentication & Authorization
│       ├── Data/               # Data access layer
│       └── Services/           # Business logic services
├── Core/
│   └── EduShield.Core/         # Core domain project
│       ├── Data/               # Entity Framework context
│       ├── Dtos/               # Data transfer objects
│       ├── Entities/           # Domain entities
│       ├── Enums/              # Enumerations
│       ├── Interfaces/         # Repository and service interfaces
│       ├── Mapping/            # AutoMapper profiles
│       └── Validators/         # FluentValidation validators
tests/
└── Api/
    └── EduShield.Api.Tests/    # API integration tests
```

## Configuration

### Development
- Uses development authentication (bypasses JWT validation)
- Connects to local PostgreSQL instance
- Detailed logging enabled

### Production
- JWT authentication with AWS Cognito
- Secure database connections
- Minimal logging

## Database

The application uses PostgreSQL with the following main entities:
- **Students**: Basic student information
- **Faculty**: Teacher and staff information
- **Performance**: Academic performance records
- **Fees**: Financial records

## Authentication

### Development Mode
- No authentication required
- All endpoints accessible
- Simulates SchoolAdmin role

### Production Mode
- JWT Bearer token authentication
- Role-based authorization
- AWS Cognito integration

## Testing

```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test tests/Api/EduShield.Api.Tests/
```

## Troubleshooting

### Database Connection Issues
1. Ensure PostgreSQL is running: `docker-compose ps`
2. Check connection string in `appsettings.Development.json`
3. Verify database exists: `docker exec -it edushield-postgres psql -U postgres -d edushield`

### Build Issues
1. Clean solution: `dotnet clean`
2. Restore packages: `dotnet restore`
3. Rebuild: `dotnet build`

### Runtime Issues
1. Check logs for detailed error messages
2. Verify all required services are running
3. Check health endpoint: `/healthz`

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests
5. Submit a pull request

## License

This project is licensed under the MIT License.
