#!/bin/bash
echo "============================================="
echo "Fixing Chrono Track database in Docker"
echo "============================================="

# Set PostgreSQL connection details - modify these to match your Docker container
export PGHOST=localhost
export PGPORT=5432
export PGDATABASE=chronotrack
export PGUSER=postgres
export PGPASSWORD=postgres

echo "Executing PostgreSQL fixes..."

# Execute the SQL fix script
cat postgres_fix.sql | psql -h $PGHOST -p $PGPORT -d $PGDATABASE -U $PGUSER

echo "Done! Database should now be fixed." 