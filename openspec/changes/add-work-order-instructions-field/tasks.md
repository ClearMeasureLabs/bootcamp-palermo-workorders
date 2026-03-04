## 1. Domain and persistence

- [x] 1.1 Add optional `Instructions` property to `WorkOrder` with max-length truncation behavior
- [x] 1.2 Update EF mapping to configure `Instructions` with `HasMaxLength(4000)`
- [x] 1.3 Add DB migration script to add nullable `Instructions` column to `dbo.WorkOrder`

## 2. UI and form handling

- [x] 2.1 Add `Instructions` to `WorkOrderManageModel`
- [x] 2.2 Add `Instructions` input under Description in `WorkOrderManage.razor`
- [x] 2.3 Load/save `Instructions` in `WorkOrderManage.razor.cs`

## 3. Tooling and integration surfaces

- [x] 3.1 Add optional instructions parameter to MCP `create-work-order`
- [x] 3.2 Include instructions in MCP work order detail output

## 4. Test updates

- [x] 4.1 Update unit tests for WorkOrder defaults/setters/truncation
- [x] 4.2 Update integration tests for mapping/save and MCP tool behavior
- [x] 4.3 Update acceptance tests that verify work order edit values

## 5. Verification

- [ ] 5.1 Run `PrivateBuild.ps1` and confirm all tests pass
