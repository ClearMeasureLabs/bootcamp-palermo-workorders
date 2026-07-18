## Why
As the API evolves, breaking changes to request/response contracts will disrupt existing integrations. URL path versioning enables introducing new API versions while maintaining backward compatibility, giving integration partners a stable contract and a clear migration path.

## What Changes
- Add API versioning configuration in `src/UI/Server/Program.cs` using `Asp.Versioning.Mvc` with URL segment strategy (`/api/v1/`, `/api/v2/`)
- Move existing API controllers under `v1` namespace in `src/UI/Api/V1/`
- Add versioned response DTOs: `WorkOrderV1Dto` in `src/UI/Api/V1/Models/` mirroring current contract
- Add `v2` controller namespace placeholder in `src/UI/Api/V2/` with `WorkOrderV2Dto` adding new fields
- Apply `[ApiVersion("1.0")]` and `[ApiVersion("2.0")]` attributes to respective controllers
- Configure default version as `1.0` for requests without explicit version
- Add `api-supported-versions` and `api-deprecated-versions` response headers

> **Note:** `Asp.Versioning.Mvc` is delivered via NuGet (not part of the base .NET SDK) and requires explicit package approval per project conventions before implementation.

## Capabilities
### New Capabilities
- URL path-based API versioning: `/api/v1/workorders` and `/api/v2/workorders`
- Versioned response DTOs decoupling API contracts from internal domain models
- Default version fallback for unversioned requests
- Version information headers in API responses

### Modified Capabilities
- Existing API endpoints reorganized under `/api/v1/` path prefix

## Impact
- **src/UI/Api/** - Restructured into `V1/` and `V2/` subdirectories with versioned controllers and DTOs
- **src/UI/Server/** - API versioning services registered in `Program.cs`
- **src/Core/** - No changes
- **src/DataAccess/** - No changes
- **Dependencies** - Requires `Asp.Versioning.Mvc` NuGet package (not part of the base .NET SDK; needs explicit approval per project conventions)

## Acceptance Criteria
### Unit Tests
- `ApiVersioning_V1Route_ReturnsV1Response` - Request to `/api/v1/workorders` returns WorkOrderV1Dto shape
- `ApiVersioning_V2Route_ReturnsV2Response` - Request to `/api/v2/workorders` returns WorkOrderV2Dto shape
- `ApiVersioning_NoVersion_DefaultsToV1` - Request to `/api/workorders` without version returns V1 response
- `ApiVersioning_InvalidVersion_Returns400` - Request to `/api/v99/workorders` returns 400 Bad Request
- `ApiVersioning_ResponseHeaders_ContainVersionInfo` - Response includes `api-supported-versions` header

### Integration Tests
- `ApiVersioning_V1AndV2_ReturnDifferentDtoShapes` - Call both versioned endpoints with same work order, verify response structures differ
- `ApiVersioning_V1Backward_Compatible_WithExistingClients` - Existing API call patterns continue working under `/api/v1/`

### Acceptance Tests
- `ApiVersioning_V1Endpoint_ReturnsExpectedData` - Use Playwright API context to call `/api/v1/workorders`, verify response contains expected fields
- `ApiVersioning_V2Endpoint_ReturnsExtendedData` - Use Playwright API context to call `/api/v2/workorders`, verify response contains additional V2 fields
- `ApiVersioning_UnversionedUrl_StillWorks` - Call `/api/workorders` without version prefix, verify successful response
