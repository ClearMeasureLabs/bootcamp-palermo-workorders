## Why
API consumers need accurate, interactive documentation to understand available endpoints, request formats, and authentication requirements. Swagger/OpenAPI documentation reduces integration time, eliminates guesswork about API contracts, and serves as a living reference that stays synchronized with the actual codebase.

## What Changes
- Add Swagger generation services in `src/UI/Server/Program.cs` using `Swashbuckle.AspNetCore` or built-in OpenAPI support in .NET 10
- Configure OpenAPI document generation with title "Work Order Management API", version info, and server URLs
- Add Swagger UI middleware serving interactive documentation at `/swagger`
- Annotate API controllers with XML documentation comments for endpoint descriptions
- Add `[ProducesResponseType]` attributes to controller actions for accurate response documentation
- Document authentication requirements (API key header) in OpenAPI security scheme definition
- Enable XML documentation file generation in API project build settings

> **Note:** `Swashbuckle.AspNetCore` is not currently in the project dependencies and would require explicit NuGet package approval per project conventions. Alternatively, .NET 10's built-in OpenAPI support (`Microsoft.AspNetCore.OpenApi`) may be used without an additional package. The `[SwaggerRequestExample]` and `[SwaggerResponseExample]` attributes require a separate package (`Swashbuckle.AspNetCore.Filters`) and are not recommended; use XML documentation comments and `[ProducesResponseType]` attributes instead for documenting request/response shapes.

## Capabilities
### New Capabilities
- Interactive Swagger UI page at `/swagger` for exploring and testing API endpoints
- Auto-generated OpenAPI 3.0 specification at `/swagger/v1/swagger.json`
- Request and response examples embedded in documentation
- Authentication requirements documented with security scheme definitions
- XML documentation comments rendered as endpoint descriptions

### Modified Capabilities
- None

## Impact
- **src/UI/Server/** - Swagger services and middleware registered in `Program.cs`
- **src/UI/Api/** - XML documentation comments and response type attributes added to controllers
- **src/UI/Api/UI.Api.csproj** - Enable `GenerateDocumentationFile` in build properties
- **Dependencies** - Requires `Swashbuckle.AspNetCore` NuGet package (needs explicit approval per project conventions), or can use built-in .NET 10 `Microsoft.AspNetCore.OpenApi` without additional packages

## Acceptance Criteria
### Unit Tests
- `SwaggerDoc_Generation_ProducesValidOpenApiJson` - Generated OpenAPI JSON is valid and parseable
- `SwaggerDoc_Endpoints_AllApiRoutesDocumented` - Every API controller action appears in the generated spec
- `SwaggerDoc_SecurityScheme_IncludesApiKeyDefinition` - OpenAPI spec contains API key security scheme
- `SwaggerDoc_ResponseTypes_MatchControllerAttributes` - Documented response types match `[ProducesResponseType]` attributes

### Integration Tests
- `SwaggerEndpoint_ReturnsValidJson` - GET `/swagger/v1/swagger.json` returns 200 with valid JSON
- `SwaggerEndpoint_ContainsAllPaths` - JSON paths object contains entries for all registered API routes
- `SwaggerUi_ReturnsHtmlPage` - GET `/swagger` returns 200 with HTML content type

### Acceptance Tests
- `SwaggerPage_Loads_WithoutAuthentication` - Navigate to `/swagger` in Playwright, verify page renders with API documentation
- `SwaggerPage_ListsAllEndpoints_WithDescriptions` - Navigate to `/swagger`, verify all API endpoints are listed with descriptions
- `SwaggerPage_TryItOut_ExecutesRequest` - Use Swagger "Try it out" feature via Playwright to send a test request, verify response is displayed
