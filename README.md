# .NET Challenge - CNAB Bycoders_

**Author:** Filipe Cotrim Melo

Web application to import CNAB files, normalize and display financial transactions by store, with balance totals. Built with ASP.NET Core, PostgreSQL, Docker, and xUnit.

---

## First Time Setup

```sh
# Build the image
docker-compose build

# Start the containers (web and db)
docker-compose up -d

# Wait a few seconds for the database to be ready, then create and migrate the database
docker-compose exec web dotnet ef database update --project src/Challenge.Web/Challenge.Web.csproj
```

Access: [http://localhost:5000](http://localhost:5000)

---

## Running Tests

```sh
# Run all tests
docker-compose exec web dotnet test src/Challenge.Tests/Challenge.Tests.csproj

# Run tests with verbose output
docker-compose exec web dotnet test src/Challenge.Tests/Challenge.Tests.csproj --verbosity normal
```

---

## Code Coverage

### Unix/Linux/macOS (using script)

```sh
./generate-coverage-report.sh
```

This script will:
1. Run tests with coverage
2. Generate an HTML report
3. Copy the report to `./coverage-report/` on your host machine
4. Open the report in your default browser

### Windows (manual steps)

```sh
# Run tests with coverage
docker-compose exec web dotnet test src/Challenge.Tests/Challenge.Tests.csproj \
  --settings src/Challenge.Tests/Challenge.Tests.runsettings \
  --collect:"XPlat Code Coverage"

# Generate HTML report
docker-compose exec web bash -c "
  COVERAGE_FILE=\$(find src/Challenge.Tests/TestResults -name 'coverage.cobertura.xml' -type f -exec stat -c '%Y %n' {} \; | sort -rn | head -1 | cut -d' ' -f2-) && \
  reportgenerator \
    -reports:\"\$COVERAGE_FILE\" \
    -targetdir:src/Challenge.Tests/TestResults/coverage-report \
    -reporttypes:Html
"

# Copy report to host (adjust container name if needed)
docker cp challenge-dotnet-web-1:/app/src/Challenge.Tests/TestResults/coverage-report ./coverage-report
```

**Note:** Coverage excludes `Program.cs`, Views (`*.cshtml`), and Migrations to focus on business logic coverage.

---

## Project Structure

```
challenge-dotnet/
├── src/
│   ├── Challenge.Web/          # Main web application
│   │   ├── Controllers/        # MVC controllers
│   │   ├── Data/               # Entity Framework DbContext
│   │   ├── Models/             # Domain models
│   │   ├── Services/           # Business logic (CNAB parser, importer)
│   │   ├── Views/              # Razor views
│   │   ├── wwwroot/            # Static files (CSS, JS)
│   │   └── Migrations/         # EF Core migrations
│   └── Challenge.Tests/        # Test project (xUnit)
│       ├── Controllers/        # Controller tests
│       ├── Models/             # Model tests
│       └── Services/           # Service tests
├── Dockerfile                  # Development Docker image
├── Dockerfile.prod            # Production Docker image
├── docker-compose.yml         # Docker Compose configuration
└── README.md                  # This file
```

---

## How to Use

1. Upload the CNAB.txt file on the main screen.
2. View stores, transactions and balances in "Stores".
3. Use the "Configuration" menu to reset the database (type CONFIRM).

---

## Development Commands

### Start/Stop the application

```sh
# Start all services
docker-compose up -d

# Stop all services
docker-compose down

# Stop and remove volumes (clears database)
docker-compose down -v
```

### Database operations

```sh
# Run Entity Framework migrations
docker-compose exec web dotnet ef database update --project src/Challenge.Web/Challenge.Web.csproj

# Create a new migration
docker-compose exec web dotnet ef migrations add MigrationName --project src/Challenge.Web/Challenge.Web.csproj

# Access PostgreSQL directly
docker-compose exec db psql -U postgres -d challenge_dev
```

### View logs

```sh
docker-compose logs -f web
```

---

## Troubleshooting

### Port already in use

If port 5000 is already in use, change it in `docker-compose.yml`:
```yaml
ports:
  - "5001:5000"  # Use port 5001 on host
```

### Database connection issues

Ensure the database container is running:
```sh
docker-compose ps
docker-compose logs db
```

### Reset everything

```sh
# Stop containers and remove volumes
docker-compose down -v

# Rebuild and start
docker-compose build
docker-compose up -d

# Recreate database
docker-compose exec web dotnet ef database update --project src/Challenge.Web/Challenge.Web.csproj
```

---

## Notes

- Database reset clears all stores and transactions and restarts IDs.
- The project uses vanilla CSS (no CSS frameworks).
- Test coverage target >80%.
- All development and testing is done through Docker containers.
