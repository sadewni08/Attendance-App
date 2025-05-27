# ChronoTrack - Attendance Management System

A modern .NET Core-based attendance tracking system that provides robust user management and attendance tracking capabilities through a RESTful API.

## Prerequisites

- .NET 7.0 SDK or later
- PostgreSQL 13.0 or later
- Visual Studio 2022, VS Code, or any preferred IDE
- Git
- Docker and Docker Compose (for containerized deployment)

## Quick Start with Docker

1. Clone the repository:
```bash
git clone [repository-url]
cd ChronoTrack
```

2. Build and run with Docker Compose:
```bash
# Build the containers
docker-compose build

# Start the services
docker-compose up -d

# View logs
docker-compose logs -f
```

## Database Configuration

1. Install PostgreSQL if not already installed
2. Create a new database for the project
3. Update the connection string in `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=chronotrack;Username=your_username;Password=your_password"
  }
}
```

## Local Development Setup

1. Restore dependencies:
```bash
dotnet restore
```

2. Apply database migrations:
```bash
dotnet ef database update
```

3. Run the application:
```bash
dotnet run
```

The API will be available at:
- Development: hhttps://localhost:7222
- OpenAPI Documentation: https://localhost:7222/scalar/v1

## Docker Commands Reference

### Basic Commands
```bash
# Build the Docker images
docker-compose build

# Start the services
docker-compose up -d

# Stop the services
docker-compose down

# Rebuild and restart services
docker-compose up -d --build

# View logs
docker-compose logs -f

# View logs for specific service
docker-compose logs -f api
```

### Container Management
```bash
# List running containers
docker ps

# Stop all containers
docker-compose stop

# Remove all containers and networks
docker-compose down

# Remove all containers, networks, and volumes
docker-compose down -v

# Restart a specific service
docker-compose restart api
```

### Debugging
```bash
# Check container health
docker inspect api

# Access container shell
docker exec -it api /bin/bash

# View container resource usage
docker stats

# View service logs
docker-compose logs -f api
```

### Database Management
```bash
# Access PostgreSQL container
docker exec -it postgres psql -U postgres -d ChronoTrack

# Backup database
docker exec -t postgres pg_dump -U postgres ChronoTrack > backup.sql

# Restore database
docker exec -i postgres psql -U postgres ChronoTrack < backup.sql
```

## Features

- 👥 User Management
- ⏰ Attendance Tracking
- 🔐 Authentication & Authorization
- 📊 RESTful API Endpoints
- 📝 OpenAPI Documentation
- 🔄 Automatic DateTime UTC Conversion
- ⚡ Entity Framework Core with PostgreSQL
- 🛠️ Development Environment Seeding
- 🐳 Docker Support

## API Endpoints

### User Endpoints
- GET /users - List all users
- POST /users - Create new user
- GET /users/{id} - Get user details
- PUT /users/{id} - Update user
- DELETE /users/{id} - Delete user

### Attendance Endpoints
- GET /attendance - List attendance records
- POST /attendance - Create attendance record
- GET /attendance/{id} - Get attendance details
- PUT /attendance/{id} - Update attendance
- DELETE /attendance/{id} - Delete attendance

## Development

The project includes:
- Automatic database creation in development
- OpenAPI documentation
- DateTime conversion middleware
- JSON serialization configuration
- Error handling with Problem Details

## Environment Configuration

The application supports different environments:
- Development
- Production

Environment-specific settings can be configured in:
- `appsettings.json`
- `appsettings.Development.json`
- `appsettings.Production.json`

## Error Handling

The application uses Problem Details for standardized error responses. All errors follow the RFC 7807 specification.



## License

This project is licensed under the MIT License - see the LICENSE file for details.

