## Why
External systems integrating with the Work Order API need a secure, manageable authentication mechanism that does not require interactive login. API key authentication provides a simple, revocable credential model suitable for server-to-server communication and third-party integrations.

## What Changes
- Add `ApiKey` entity in `src/Core/Model/` with properties: Id, Key (hashed), ClientName, CreatedDate, ExpirationDate, IsActive
- Add `ApiKeyConfiguration` EF Core entity configuration in `src/DataAccess/`
- Add database migration script in `src/Database/scripts/Update/` to create the `ApiKey` table
- Add `ApiKeyAuthenticationHandler` in `src/UI/Server/Authentication/` implementing `AuthenticationHandler<AuthenticationSchemeOptions>`
- Add `ApiKeyValidationQuery` in `src/Core/Queries/` and corresponding handler in `src/DataAccess/Handlers/`
- Register the authentication scheme in `UIServiceRegistry.cs`
- Validate `X-Api-Key` header on each API request; return 401 Unauthorized for missing or invalid keys

## Capabilities
### New Capabilities
- Authenticate API requests via `X-Api-Key` request header
- Store and manage API keys in the database with hashed values
- Support API key expiration and revocation via IsActive flag
- Return 401 Unauthorized for requests with missing, invalid, or expired API keys

### Modified Capabilities
- None

## Impact
- **src/Core/Model/** - New `ApiKey` entity
- **src/Core/Queries/** - New `ApiKeyValidationQuery`
- **src/DataAccess/** - New entity configuration, new query handler, updated `DataContext` with `DbSet<ApiKey>`
- **src/Database/** - New migration script for `ApiKey` table
- **src/UI/Server/** - New authentication handler and scheme registration
- **Dependencies** - No new NuGet packages required

## Acceptance Criteria
### Unit Tests
- `ApiKeyAuth_ValidKey_AuthenticatesSuccessfully` - Request with valid API key passes authentication
- `ApiKeyAuth_MissingHeader_Returns401` - Request without X-Api-Key header returns 401
- `ApiKeyAuth_InvalidKey_Returns401` - Request with non-existent key returns 401
- `ApiKeyAuth_ExpiredKey_Returns401` - Request with expired API key returns 401
- `ApiKeyAuth_InactiveKey_Returns401` - Request with revoked (IsActive=false) key returns 401
- `ApiKey_KeyHashing_DoesNotStoreRawValue` - Stored ApiKey.Key value does not match raw key input

### Integration Tests
- `ApiKeyAuth_CreateAndAuthenticate_Succeeds` - Insert an API key into the database, send request with that key, verify authentication passes
- `ApiKeyAuth_RevokeKey_BlocksSubsequentRequests` - Authenticate with valid key, set IsActive to false, verify next request returns 401
- `ApiKeyAuth_ExpiredKey_DeniesAccess` - Insert API key with past expiration date, verify request returns 401

### Acceptance Tests
- `Api_WithoutApiKey_Returns401` - Send API request via Playwright without X-Api-Key header, verify 401 response
- `Api_WithValidApiKey_ReturnsData` - Send API request with valid key, verify successful response with expected data
- `Api_WithRevokedApiKey_Returns401` - Revoke key in database, send request, verify 401 response
