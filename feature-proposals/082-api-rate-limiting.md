## Why
Uncontrolled API usage can degrade system performance and availability for all users. Rate limiting protects the application from abuse, ensures fair resource allocation across clients, and provides predictable throughput guarantees for integration partners.

## What Changes
- Add rate limiting middleware in `src/UI/Server/Middleware/RateLimitingMiddleware.cs` using the built-in `System.Threading.RateLimiting` APIs
- Configure per-client rate limits in `appsettings.json` under a `RateLimiting` section (requests per window, window duration, queue limit)
- Return HTTP 429 Too Many Requests with `Retry-After` header when limits are exceeded
- Add `X-RateLimit-Limit`, `X-RateLimit-Remaining`, and `X-RateLimit-Reset` response headers to all API responses
- Register rate limiting services in `UIServiceRegistry.cs`
- Apply rate limiting policy to API controller routes via `[EnableRateLimiting]` attribute

## Capabilities
### New Capabilities
- Per-client request throttling based on client identifier (IP address or API key)
- Configurable rate limit windows and thresholds via application settings
- Standard rate limit response headers on every API response
- HTTP 429 response with Retry-After header when limit exceeded

### Modified Capabilities
- None

## Impact
- **src/UI/Server/** - New middleware class, updated `Program.cs` to register rate limiting services
- **src/UI/Server/appsettings.json** - New `RateLimiting` configuration section
- **src/UI/Api/** - Rate limiting attribute applied to API controllers
- **Dependencies** - No new NuGet packages; `System.Threading.RateLimiting` is included in .NET 10

## Acceptance Criteria
### Unit Tests
- `RateLimiting_UnderLimit_AllowsRequest` - Requests within limit return normal status codes
- `RateLimiting_ExceedsLimit_Returns429` - Request exceeding limit returns 429 Too Many Requests
- `RateLimiting_ResponseHeaders_IncludeRateLimitInfo` - Every response includes X-RateLimit-Limit and X-RateLimit-Remaining headers
- `RateLimiting_RetryAfterHeader_PresentOn429` - 429 response includes Retry-After header with correct value
- `RateLimiting_DifferentClients_TrackedSeparately` - Two different clients each get independent rate limit counters

### Integration Tests
- `RateLimiting_BurstRequests_EnforcesLimit` - Send requests exceeding configured limit in rapid succession, verify 429 returned after threshold
- `RateLimiting_WindowReset_AllowsNewRequests` - Exhaust rate limit, wait for window reset, verify subsequent requests succeed
- `RateLimiting_ConfigurationChange_AppliesNewLimits` - Verify different configuration values produce different rate limit behavior

### Acceptance Tests
- `Api_RapidRequests_EventuallyReturns429` - Use Playwright API context to send rapid sequential requests, verify 429 is returned
- `Api_RateLimitHeaders_PresentOnEveryResponse` - Send API request via Playwright, verify X-RateLimit headers present in response
