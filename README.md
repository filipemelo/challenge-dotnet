# .NET Challenge - CNAB Bycoders_

## Description

Web application to import CNAB files, normalize and display financial transactions by store, with balance totals. Built with ASP.NET Core, PostgreSQL, Docker, and xUnit.

---

## Requirements

- Docker and Docker Compose (recommended)
- PostgreSQL (included in Docker Compose)

---

## Quick Setup with Docker

```sh
# Build the image
docker-compose build

# Start the containers (web and db)
docker-compose up -d

# Wait a few seconds for the database to be ready, then create and migrate the database
docker-compose exec web dotnet ef database update --project src/Challenge.Web/Challenge.Web.csproj
```

Access: [http://localhost:5000](http://localhost:5000)

**Note:** If you see "service 'web' is not running", make sure you've run `docker-compose up -d` first. You can check the status with `docker-compose ps`.

---

## Development Workflow

### Start the application

```sh
# Start all services
docker-compose up

# Or run in detached mode
docker-compose up -d

# View logs
docker-compose logs -f web
```

### Stop the application

```sh
# Stop all services
docker-compose down

# Stop and remove volumes (clears database)
docker-compose down -v
```

### Run commands inside the container

```sh
# Open a shell in the web container
docker-compose exec web bash

# Run .NET CLI commands
docker-compose exec web dotnet --version
docker-compose exec web dotnet restore
docker-compose exec web dotnet build src/Challenge.Web/Challenge.Web.csproj
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

---

## How to Use

1. Upload the CNAB.txt file on the main screen.
2. View stores, transactions and balances in "Stores".
3. Use the "Configuration" menu to reset the database (type CONFIRMAR).

---

## Tests

All tests are run inside Docker containers.

### Run all tests

```sh
docker-compose exec web dotnet test src/Challenge.Tests/Challenge.Tests.csproj
```

### Run tests with verbose output

```sh
docker-compose exec web dotnet test src/Challenge.Tests/Challenge.Tests.csproj --verbosity normal
```

### Run tests with coverage

```sh
# Basic coverage using XPlat Code Coverage collector (recommended)
docker-compose exec web dotnet test src/Challenge.Tests/Challenge.Tests.csproj --collect:"XPlat Code Coverage"

# Coverage with MSBuild properties (alternative)
docker-compose exec web dotnet test src/Challenge.Tests/Challenge.Tests.csproj /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura

# Coverage with threshold (fails if below threshold)
docker-compose exec web dotnet test src/Challenge.Tests/Challenge.Tests.csproj \
  /p:CollectCoverage=true \
  /p:Threshold=80 \
  /p:ThresholdType=line \
  /p:ThresholdStat=total
```

Coverage reports will be generated in `src/Challenge.Tests/TestResults/{guid}/`:
- `coverage.cobertura.xml` - Cobertura format (for CI/CD and ReportGenerator)
- Additional formats can be configured in the test project file

### Generate HTML coverage report

To generate a nice HTML report from the coverage data:

```sh
# Install ReportGenerator tool (one-time setup)
docker-compose exec web dotnet tool install --global dotnet-reportgenerator-globaltool

# Run tests with coverage
docker-compose exec web dotnet test src/Challenge.Tests/Challenge.Tests.csproj --collect:"XPlat Code Coverage"

# Find the latest coverage file and generate HTML report
docker-compose exec web bash -c "
  COVERAGE_FILE=\$(find src/Challenge.Tests/TestResults -name 'coverage.cobertura.xml' | head -1) && \
  reportgenerator \
    -reports:\"\$COVERAGE_FILE\" \
    -targetdir:src/Challenge.Tests/TestResults/coverage-report \
    -reporttypes:Html
"

# Or do it all in one command
docker-compose exec web bash -c "
  dotnet test src/Challenge.Tests/Challenge.Tests.csproj --collect:\"XPlat Code Coverage\" && \
  COVERAGE_FILE=\$(find src/Challenge.Tests/TestResults -name 'coverage.cobertura.xml' | head -1) && \
  reportgenerator -reports:\"\$COVERAGE_FILE\" -targetdir:src/Challenge.Tests/TestResults/coverage-report -reporttypes:Html
"
```

### View coverage summary

```sh
# Quick coverage summary from Cobertura XML
docker-compose exec web bash -c "
  COVERAGE_FILE=\$(find src/Challenge.Tests/TestResults -name 'coverage.cobertura.xml' | head -1) && \
  echo \"Line Coverage: \$(grep -oP 'line-rate=\"\K[0-9.]+' \"\$COVERAGE_FILE\" | head -1 | awk '{print \$1*100\"%\"}') && \
  echo \"Branch Coverage: \$(grep -oP 'branch-rate=\"\K[0-9.]+' \"\$COVERAGE_FILE\" | head -1 | awk '{print \$1*100\"%\"}') \"
"
```

### Run specific test class

```sh
docker-compose exec web dotnet test src/Challenge.Tests/Challenge.Tests.csproj --filter "FullyQualifiedName~CnabParserTests"
```

### Run tests in watch mode (requires rebuilding container)

```sh
# Build with test tools
docker-compose exec web dotnet watch test src/Challenge.Tests/Challenge.Tests.csproj
```

---

## API

- **Upload endpoint:** `POST /cnab_files`
- **Stores endpoint:** `GET /stores`
- (Additional endpoints will be documented as they are created)

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
│       ├── Models/             # Model tests
│       └── Services/           # Service tests
├── Dockerfile                  # Development Docker image
├── Dockerfile.prod            # Production Docker image
├── docker-compose.yml         # Docker Compose configuration
└── README.md                  # This file
```

---

## Notes

- Database reset clears all stores and transactions and restarts IDs.
- The project uses vanilla CSS (no CSS frameworks).
- Test coverage target >80% (current: ~37% - work in progress).
- All development and testing is done through Docker containers.
- Coverage reports are generated in `src/Challenge.Tests/TestResults/` directories.

---

## Production Deployment

To build and run the production Docker image:

```sh
# Build production image
docker build -f Dockerfile.prod -t challenge-dotnet:prod .

# Run production container
docker run -d \
  -p 80:80 \
  -e ConnectionStrings__DefaultConnection="Host=db;Port=5432;Database=challenge_prod;Username=postgres;Password=yourpassword" \
  --name challenge-app \
  challenge-dotnet:prod
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

### Test failures

If tests fail, ensure the database is properly set up:
```sh
# Rebuild test project
docker-compose exec web dotnet build src/Challenge.Tests/Challenge.Tests.csproj

# Run tests with detailed output
docker-compose exec web dotnet test src/Challenge.Tests/Challenge.Tests.csproj --verbosity detailed
```
