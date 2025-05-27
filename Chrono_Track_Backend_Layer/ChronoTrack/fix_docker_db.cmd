@echo off
echo =============================================
echo Fixing Chrono Track database in Docker
echo =============================================

REM Set PostgreSQL connection details - modify these to match your Docker container
set PGHOST=localhost
set PGPORT=5432
set PGDATABASE=chronotrack
set PGUSER=postgres
set PGPASSWORD=postgres

echo Executing PostgreSQL fixes...

REM Execute the SQL fix script
type postgres_fix.sql | psql -h %PGHOST% -p %PGPORT% -d %PGDATABASE% -U %PGUSER%

echo Done! Database should now be fixed.
pause 