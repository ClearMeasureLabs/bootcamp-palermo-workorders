# AI Factory Executor - Docker Image
# Multi-stage build for optimal image size

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files
COPY executor.csproj .
COPY executor.cs .

# Restore dependencies
RUN dotnet restore executor.csproj

# Build and publish
RUN dotnet publish executor.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    -p:PublishSingleFile=true \
    -p:PublishTrimmed=false \
    --self-contained false

# Runtime stage
FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime

# Install git and GitHub CLI
RUN apt-get update && \
    apt-get install -y --no-install-recommends \
        git \
        curl \
        ca-certificates && \
    # Install GitHub CLI
    curl -fsSL https://cli.github.com/packages/githubcli-archive-keyring.gpg | dd of=/usr/share/keyrings/githubcli-archive-keyring.gpg && \
    chmod go+r /usr/share/keyrings/githubcli-archive-keyring.gpg && \
    echo "deb [arch=$(dpkg --print-architecture) signed-by=/usr/share/keyrings/githubcli-archive-keyring.gpg] https://cli.github.com/packages stable main" | tee /etc/apt/sources.list.d/github-cli.list > /dev/null && \
    apt-get update && \
    apt-get install -y gh && \
    # Clean up
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

WORKDIR /app

# Copy published application
COPY --from=build /app/publish .

# Set git safe directory (for mounted volumes)
RUN git config --global --add safe.directory '*'

# Default working directory for repository
WORKDIR /workspace

# Entry point
ENTRYPOINT ["/app/ai-factory-executor"]

# Default command (can be overridden)
CMD ["--help"]
