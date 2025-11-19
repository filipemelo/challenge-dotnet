# Dockerfile for development container
FROM mcr.microsoft.com/dotnet/sdk:8.0

# Install PostgreSQL client tools (optional, useful for debugging)
RUN apt-get update -qq && \
    apt-get install -y postgresql-client && \
    rm -rf /var/lib/apt/lists/*

# Set working directory
WORKDIR /app

# Expose port 5000
EXPOSE 5000

