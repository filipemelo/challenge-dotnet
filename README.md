# .NET Challenge - CNAB Bycoders_

## Description

Web application to import CNAB files, normalize and display financial transactions by store, with balance totals. Built with ASP.NET Core, PostgreSQL, Docker, and xUnit.

---

## Requirements

- .NET 8 SDK (or use Docker)
- PostgreSQL
- Docker and Docker Compose (recommended)

---

## Quick Setup with Docker

```sh
# Build the image
docker-compose build

# Start the containers (web and db)
docker-compose up -d

# Wait a few seconds for the database to be ready, then create and migrate the database
# (This step is only needed if you have Entity Framework migrations set up)
docker-compose exec web dotnet ef database update --project Challenge.csproj
```

Access: [http://localhost:5001](http://localhost:5001)

**Note:** Port 5001 is used instead of 5000 to avoid conflicts with macOS AirPlay Receiver.

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
docker-compose exec web dotnet build
docker-compose exec web dotnet test
```

### Database operations

```sh
# Run Entity Framework migrations
docker-compose exec web dotnet ef database update --project Challenge.csproj

# Create a new migration
docker-compose exec web dotnet ef migrations add MigrationName --project Challenge.csproj

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

- **Run tests:**
  ```sh
  docker-compose exec web dotnet test
  ```

- **Run tests with coverage:**
  ```sh
  docker-compose exec web dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
  ```

---

## API

- **Upload endpoint:** `POST /cnab_files`
- **Stores endpoint:** `GET /stores`
- (Additional endpoints will be documented as they are created)

---

## Project Structure

- `Models/` - Main models: Store, Transaction, CnabFile
- `Services/` - Import logic and CNAB parser
- `Controllers/` - Controllers for upload, stores and configuration
- `Tests/` - Automated tests (xUnit)

---

## Notes

- Database reset clears all stores and transactions and restarts IDs.
- The project uses vanilla CSS (no CSS frameworks).
- Test coverage target >80%.

---

## How to Run Locally (without Docker)

1. Install .NET 8 SDK and PostgreSQL.
2. Clone the project and configure the connection string in `appsettings.json`:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Host=localhost;Port=5432;Database=challenge_dev;Username=postgres;Password=postgres"
     }
   }
   ```
3. Run the application:
   ```sh
   dotnet restore
   dotnet ef database update
   dotnet run
   ```

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

If port 5001 is already in use, change it in `docker-compose.yml`:
```yaml
ports:
  - "5002:5000"  # Use port 5002 on host
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
```

