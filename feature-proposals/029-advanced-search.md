## Why
Power users need to construct complex queries combining multiple field conditions with AND/OR logic to find specific subsets of work orders. An advanced search panel enables precise filtering that goes beyond simple single-field filters, supporting audit, compliance, and detailed analysis workflows.

## What Changes
- Create an `AdvancedSearchCriteria` model in `src/Core/Queries/` with properties: Field, Operator (Equals, Contains, GreaterThan, LessThan), Value, and LogicalOperator (AND, OR)
- Add an `AdvancedSearchCriteriaList` property to `WorkOrderSpecificationQuery` in `src/Core/Queries/`
- Update the query handler in `src/DataAccess/Handlers/` to build dynamic WHERE clauses from the criteria list
- Create a collapsible advanced search panel component in `src/UI/Client/` with field selector, operator selector, value input, and AND/OR toggle
- Allow users to add and remove condition rows dynamically
- Add a "Search" button that executes the composed query
- Wire the panel through the API controller in `src/UI/Api/`

## Capabilities
### New Capabilities
- Collapsible advanced search panel on the WorkOrderSearch page
- Dynamic condition builder with field, operator, and value selectors
- AND/OR logic toggles between conditions
- Support for text, date, and status field comparisons
- Add and remove condition rows dynamically

### Modified Capabilities
- WorkOrderSearch page includes a toggleable "Advanced Search" section below existing filters
- WorkOrderSpecificationQuery supports a list of advanced search criteria

## Impact
- `src/Core/Queries/WorkOrderSpecificationQuery.cs` — new AdvancedSearchCriteriaList property
- `src/Core/Queries/AdvancedSearchCriteria.cs` — new model for criteria definition
- `src/DataAccess/Handlers/` — updated query handler with dynamic WHERE clause builder
- `src/UI/Client/` — new advanced search panel component, updated WorkOrderSearch page
- `src/UI/Api/` — updated API controller to accept advanced criteria
- No database migration required
- No new NuGet packages required

## Acceptance Criteria
### Unit Tests
- Handler builds correct WHERE clause for a single Equals condition
- Handler builds correct WHERE clause for a single Contains condition
- Handler combines multiple conditions with AND logic correctly
- Handler combines multiple conditions with OR logic correctly
- Handler handles mixed AND/OR conditions with proper precedence
- Invalid field names are rejected with a validation error

### Integration Tests
- Advanced search with multiple AND conditions returns correct results from a seeded database
- Advanced search with OR conditions returns the union of matching results
- Date comparison operators filter correctly

### Acceptance Tests
- User expands the advanced search panel by clicking "Advanced Search"
- User adds a condition, selects a field, operator, and value, then clicks Search and sees filtered results
- User adds multiple conditions with AND logic and results match all conditions
- User removes a condition row and the remaining conditions still function
- User collapses the advanced search panel and it retains the entered conditions
