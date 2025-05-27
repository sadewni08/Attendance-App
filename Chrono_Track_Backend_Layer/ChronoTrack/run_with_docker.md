# Running Chrono Track with Docker PostgreSQL

This guide explains how to set up and run Chrono Track with a containerized PostgreSQL database.

## Prerequisites

- Docker installed on your system
- Docker Compose (optional, but recommended)
- .NET SDK 9.0 or later

## Setup PostgreSQL Container

### Using Docker Compose (Recommended)

1. Create a `docker-compose.yml` file in the root directory:

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

### Using Docker CLI

```bash
docker run --name chronotrack-postgres -e POSTGRES_PASSWORD=postgres -e POSTGRES_USER=postgres -e POSTGRES_DB=chronotrack -p 5432:5432 -d postgres
```

## Configure Connection String

Make sure your connection string in `appsettings.json` is correctly set:

```json
"ConnectionStrings": {
  "DefaultConnection": "Host=localhost;Port=5432;Database=chronotrack;Username=postgres;Password=postgres"
}
```

## Fix Database Issues

If you encounter database issues when using Docker PostgreSQL, you can run one of the provided fix scripts:

### Windows

Run the `fix_docker_db.cmd` script:

```
cd Chrono_Track_Backend_Layer/ChronoTrack
fix_docker_db.cmd
```

### Linux/Mac

Run the `fix_docker_db.sh` script:

```bash
cd Chrono_Track_Backend_Layer/ChronoTrack
chmod +x fix_docker_db.sh
./fix_docker_db.sh
```

## Known Issues and Solutions

### Shadow Properties

The application may have issues with shadow properties in Entity Framework Core. The fix scripts address this by removing duplicate columns and ensuring data consistency.

### Case Sensitivity in PostgreSQL

PostgreSQL is case-sensitive with column names, but Entity Framework Core may not handle this correctly. The fix scripts ensure proper column casing.

### -infinity Values

Some date fields might have `-infinity` values which can cause issues. The fix scripts update these to proper timestamps.

## Manual SQL Fixes

If needed, you can directly execute the `postgres_fix.sql` script against your PostgreSQL database:

```bash
psql -h localhost -p 5432 -d chronotrack -U postgres -f postgres_fix.sql
``` 