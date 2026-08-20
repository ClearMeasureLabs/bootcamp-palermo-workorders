# AI Factory Executor - Docker Guide

Complete guide for running the AI Factory Executor in Docker containers.

## Quick Start

```bash
# Navigate to skill directory
cd .bob/skills/ai-factory-executor

# Build the image
docker build -t ai-factory-executor:latest .

# Run with docker-compose (recommended)
docker-compose run ai-factory-executor --dry-run

# Or run directly
docker run --rm \
  -v ~/.config/gh:/root/.config/gh:ro \
  -v $(pwd)/../../..:/workspace \
  ai-factory-executor:latest --dry-run
```

## Dockerfile

The multi-stage Dockerfile provides:
- **Build stage**: Uses .NET 10 SDK to compile the application
- **Runtime stage**: Minimal .NET 10 runtime with git and GitHub CLI
- **Single-file executable**: Published as optimized single binary
- **Small image size**: ~200MB runtime image (vs ~1GB SDK image)

### Build Arguments

```bash
# Build with custom tag
docker build -t ai-factory-executor:v1.0.0 .

# Build with cache disabled
docker build --no-cache -t ai-factory-executor:latest .
```

## Docker Compose

The `docker-compose.yml` provides two services:

### 1. ai-factory-executor (Main Service)

```bash
# Run with default options (dry-run)
docker-compose run ai-factory-executor

# Run with custom options
docker-compose run ai-factory-executor --max-concurrent 3

# Run in background
docker-compose up -d ai-factory-executor
```

### 2. test-discovery (Test Service)

```bash
# Run discovery test
docker-compose run test-discovery
```

## Volume Mounts

### GitHub CLI Configuration (Required)

```bash
# Mount GitHub CLI config (read-only)
-v ~/.config/gh:/root/.config/gh:ro
```

**Alternative:** Use `GITHUB_TOKEN` environment variable:

```bash
docker run --rm \
  -e GITHUB_TOKEN=$GITHUB_TOKEN \
  -v $(pwd)/../../..:/workspace \
  ai-factory-executor:latest
```

### Workspace (Required)

```bash
# Mount repository root
-v $(pwd)/../../..:/workspace
```

The container expects the repository at `/workspace`.

## Environment Variables

### GitHub Authentication

```bash
# Option 1: GitHub token
-e GITHUB_TOKEN=ghp_xxxxxxxxxxxx

# Option 2: Mount gh config (preferred)
-v ~/.config/gh:/root/.config/gh:ro
```

### Git Configuration

```bash
-e GIT_AUTHOR_NAME="AI Factory Bot"
-e GIT_AUTHOR_EMAIL="ai-factory@example.com"
-e GIT_COMMITTER_NAME="AI Factory Bot"
-e GIT_COMMITTER_EMAIL="ai-factory@example.com"
```

### Application Settings

```bash
# Max concurrent subagents
-e MAX_CONCURRENT=3

# Poll interval (seconds)
-e POLL_INTERVAL=10
```

## Complete Examples

### Local Development

```bash
# Build
docker build -t ai-factory-executor:latest .

# Test discovery
docker run --rm \
  -v ~/.config/gh:/root/.config/gh:ro \
  -v $(pwd)/../../..:/workspace \
  ai-factory-executor:latest \
  dotnet run --project .bob/skills/ai-factory-executor/test-discovery.csproj

# Run executor (dry-run)
docker run --rm \
  -v ~/.config/gh:/root/.config/gh:ro \
  -v $(pwd)/../../..:/workspace \
  ai-factory-executor:latest --dry-run

# Run executor (live)
docker run --rm \
  -v ~/.config/gh:/root/.config/gh:ro \
  -v $(pwd)/../../..:/workspace \
  ai-factory-executor:latest --max-concurrent 2
```

### CI/CD (GitHub Actions)

See `.github/workflows/ai-factory-executor.yml` for complete workflow.

Key steps:
1. Build Docker image
2. Authenticate GitHub CLI in container
3. Run executor with mounted workspace
4. Upload logs as artifacts

### Production Deployment

```bash
# Build production image
docker build -t ai-factory-executor:prod .

# Run with restart policy
docker run -d \
  --name ai-factory-executor \
  --restart unless-stopped \
  -v ~/.config/gh:/root/.config/gh:ro \
  -v /path/to/repo:/workspace \
  -e GITHUB_TOKEN=$GITHUB_TOKEN \
  ai-factory-executor:prod --max-concurrent 2 --poll-interval 30
```

## Docker Compose Configuration

### Custom Configuration

Create `docker-compose.override.yml`:

```yaml
version: '3.8'

services:
  ai-factory-executor:
    environment:
      - MAX_CONCURRENT=3
      - POLL_INTERVAL=15
    command: ["--max-concurrent", "3"]
```

### Multiple Repositories

```yaml
version: '3.8'

services:
  ai-factory-repo1:
    extends:
      service: ai-factory-executor
    volumes:
      - ~/.config/gh:/root/.config/gh:ro
      - /path/to/repo1:/workspace
    command: ["--org", "MyOrg", "--repo", "repo1"]
  
  ai-factory-repo2:
    extends:
      service: ai-factory-executor
    volumes:
      - ~/.config/gh:/root/.config/gh:ro
      - /path/to/repo2:/workspace
    command: ["--org", "MyOrg", "--repo", "repo2"]
```

## Troubleshooting

### "GitHub CLI not authenticated"

```bash
# Verify gh config is mounted
docker run --rm \
  -v ~/.config/gh:/root/.config/gh:ro \
  ai-factory-executor:latest \
  gh auth status

# Or use GITHUB_TOKEN
docker run --rm \
  -e GITHUB_TOKEN=$GITHUB_TOKEN \
  ai-factory-executor:latest \
  gh auth status
```

### "Could not detect org/repo"

```bash
# Ensure workspace is mounted correctly
docker run --rm \
  -v $(pwd)/../../..:/workspace \
  ai-factory-executor:latest \
  git remote get-url origin

# Or specify explicitly
docker run --rm \
  -v ~/.config/gh:/root/.config/gh:ro \
  -v $(pwd)/../../..:/workspace \
  ai-factory-executor:latest \
  --org MyOrg --repo MyRepo
```

### Permission Issues

```bash
# Run as current user
docker run --rm \
  --user $(id -u):$(id -g) \
  -v ~/.config/gh:/root/.config/gh:ro \
  -v $(pwd)/../../..:/workspace \
  ai-factory-executor:latest
```

### Network Issues

```bash
# Use host network (Linux only)
docker run --rm \
  --network host \
  -v ~/.config/gh:/root/.config/gh:ro \
  -v $(pwd)/../../..:/workspace \
  ai-factory-executor:latest
```

## Image Management

### Build and Push to Registry

```bash
# Build
docker build -t myregistry.azurecr.io/ai-factory-executor:latest .

# Login to registry
docker login myregistry.azurecr.io

# Push
docker push myregistry.azurecr.io/ai-factory-executor:latest
```

### Multi-Platform Build

```bash
# Build for multiple platforms
docker buildx build \
  --platform linux/amd64,linux/arm64 \
  -t ai-factory-executor:latest \
  --push .
```

### Image Cleanup

```bash
# Remove old images
docker image prune -a

# Remove specific image
docker rmi ai-factory-executor:latest
```

## Performance Optimization

### Build Cache

```bash
# Use BuildKit for better caching
DOCKER_BUILDKIT=1 docker build -t ai-factory-executor:latest .
```

### Resource Limits

```bash
# Limit CPU and memory
docker run --rm \
  --cpus=2 \
  --memory=2g \
  -v ~/.config/gh:/root/.config/gh:ro \
  -v $(pwd)/../../..:/workspace \
  ai-factory-executor:latest
```

## Security Best Practices

1. **Use read-only mounts** for sensitive data:
   ```bash
   -v ~/.config/gh:/root/.config/gh:ro
   ```

2. **Don't commit secrets** to images:
   - Use environment variables
   - Use Docker secrets
   - Mount config files

3. **Run as non-root** (when possible):
   ```bash
   --user $(id -u):$(id -g)
   ```

4. **Scan images** for vulnerabilities:
   ```bash
   docker scan ai-factory-executor:latest
   ```

## See Also

- [README.md](README.md) - Main documentation
- [SKILL.md](SKILL.md) - Skill specification
- [.github/workflows/ai-factory-executor.yml](../../.github/workflows/ai-factory-executor.yml) - GitHub Actions workflow
- [docker-compose.yml](docker-compose.yml) - Docker Compose configuration
