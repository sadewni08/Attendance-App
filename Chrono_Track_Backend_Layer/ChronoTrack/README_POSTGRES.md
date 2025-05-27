# Chrono Track PostgreSQL Integration Guide

This document provides guidance on running Chrono Track with PostgreSQL in a Docker container and troubleshooting common issues.

## Setting Up PostgreSQL in Docker

### Option 1: Using Docker Compose (Recommended)

1. Create a `docker-compose.yml` file in your project root:

```yaml
version: '3.8'

services:
  postgres:
    image: postgres:latest
    container_name: chronotrack-postgres
    environment:
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: postgres
      POSTGRES_DB: chronotrack
    ports:
      - "5432:5432"
    volumes:
      - postgres-data:/var/lib/postgresql/data
    restart: unless-stopped

volumes:
  postgres-data:
```

2. Start the container:

```bash
docker-compose up -d
```

### Option 2: Using Docker CLI

```bash
docker run --name chronotrack-postgres -e POSTGRES_PASSWORD=postgres -e POSTGRES_USER=postgres -e POSTGRES_DB=chronotrack -p 5432:5432 -d postgres
```

## Configuration

Make sure your application is configured to use PostgreSQL by setting the correct connection string in `appsettings.json`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Host=localhost;Port=5432;Database=chronotrack;Username=postgres;Password=postgres"
}
```

## Common Issues and Solutions

### 1. Shadow Properties in Users Table

**Issue**: The `Users` table may have duplicate columns (`DepartmentID`/`DepartmentId` and `UserRoleID`/`UserRoleId`) due to Entity Framework Core convention naming versus database naming.

**Solution**: The application now includes automatic fixes that will:
- Detect shadow properties
- Copy data from shadow properties to the main properties if needed
- Remove shadow properties and their constraints

### 2. Missing User Roles

**Issue**: Some user roles might be missing, causing errors like `"User Role with ID xxx doesn't exist"`.

**Solution**: The application now includes two fallback mechanisms:
1. **Direct SQL**: Automatically inserts required roles using direct SQL
2. **Service API**: Uses the application's own API to create missing roles

### 3. Invalid Date Values

**Issue**: Some date fields might have `-infinity` values which can cause errors.

**Solution**: The automatic fixes will update these invalid dates with current timestamps.

## Manual Fixes

If needed, you can manually fix database issues by:

### Using the Fix Scripts

#### Windows
```
cd Chrono_Track_Backend_Layer/ChronoTrack
fix_docker_db.cmd
```

#### Linux/Mac
```bash
cd Chrono_Track_Backend_Layer/ChronoTrack
chmod +x fix_docker_db.sh
./fix_docker_db.sh
```

### Using Direct SQL

You can also connect to your PostgreSQL instance and run the `postgres_fix.sql` script:

```bash
psql -h localhost -p 5432 -d chronotrack -U postgres -f postgres_fix.sql
```

## Debugging Database Issues

If you encounter database issues:

1. Check the application logs for detailed error messages
2. Inspect the database schema using a tool like pgAdmin or DBeaver
3. Look for specific error messages about missing columns or roles
4. Run the application with enhanced logging by modifying `appsettings.json`:
   ```json
   "Logging": {
     "LogLevel": {
       "Default": "Information",
       "Microsoft.EntityFrameworkCore": "Debug"
     }
   }
   ```

## Making Schema Changes

If you need to make schema changes:

1. Update your entity models
2. Create a new migration:
   ```
   dotnet ef migrations add YourMigrationName
   ```
3. Apply the migration:
   ```
   dotnet ef database update
   ```
   
Remember that PostgreSQL is case-sensitive, so be careful with column naming in your entity models. 