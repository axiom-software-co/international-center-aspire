#!/usr/bin/env bash

# Production SQL Script Generation for International Center
# Microsoft recommended pattern for EF Core production deployments

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"
MIGRATION_PROJECT="$PROJECT_ROOT/InternationalCenter.Migrations.Service"
OUTPUT_DIR="$PROJECT_ROOT/sql-scripts"

echo "🔧 Generating production SQL scripts..."
echo "Migration project: $MIGRATION_PROJECT"
echo "Output directory: $OUTPUT_DIR"

# Create output directory if it doesn't exist
mkdir -p "$OUTPUT_DIR"

# Change to migration project directory
cd "$MIGRATION_PROJECT"

# Generate complete migration script (all migrations)
echo "📄 Generating complete migration script..."
dotnet ef migrations script \
    --output "$OUTPUT_DIR/complete-migrations.sql" \
    --verbose

# Generate incremental script from last migration (requires database connectivity)
echo "📄 Generating incremental migration script..."
if dotnet ef migrations list &>/dev/null; then
    LAST_MIGRATION=$(dotnet ef migrations list | tail -n 1 | tr -d ' ')
    if [ ! -z "$LAST_MIGRATION" ]; then
        dotnet ef migrations script "$LAST_MIGRATION" \
            --output "$OUTPUT_DIR/incremental-migrations.sql" \
            --verbose 2>/dev/null || echo "⚠️  Incremental script requires database connectivity - skipping"
    fi
else
    echo "⚠️  Cannot list migrations without database connectivity - skipping incremental script"
fi

# Generate script with idempotent checks
echo "📄 Generating idempotent migration script..."
dotnet ef migrations script \
    --idempotent \
    --output "$OUTPUT_DIR/idempotent-migrations.sql" \
    --verbose

# Generate database creation script
echo "📄 Generating database creation script..."
dotnet ef migrations script \
    --output "$OUTPUT_DIR/database-creation.sql" \
    --verbose

echo "✅ Production SQL scripts generated successfully!"
echo ""
echo "Generated files:"
echo "  📁 $OUTPUT_DIR/complete-migrations.sql      - All migrations (complete schema)"
echo "  📁 $OUTPUT_DIR/idempotent-migrations.sql    - Safe to run multiple times"
echo "  📁 $OUTPUT_DIR/database-creation.sql        - Full database creation schema"
echo ""
echo "🚀 For production deployment:"
echo "  1. Review generated SQL scripts with DBA team"
echo "  2. Test scripts on staging environment"
echo "  3. Apply scripts to production database"
echo "  4. Deploy application with ASPNETCORE_ENVIRONMENT=Production"
echo ""
echo "📖 More info: https://docs.microsoft.com/en-us/ef/core/managing-schemas/migrations/applying"