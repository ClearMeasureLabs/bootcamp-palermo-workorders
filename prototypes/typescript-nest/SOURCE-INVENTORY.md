# Tracked source inventory

Generated from git ls-files src (765 tracked paths). Each row maps one upstream file to a NestJS rewrite area or records why it remains deferred.

Regenerate after source tree changes with:

```sh
node prototypes/typescript-nest/scripts/generate-source-inventory.mjs
```

| Tracked source path | Area | NestJS destination / disposition |
|---|---|---|
| `src/.editorconfig` | .editorconfig project | Review and map from .editorconfig:  |
| `src/.mcp.json` | .mcp.json project | Review and map from .mcp.json:  |
| `src/AcceptanceTests/.editorconfig` | Browser/system acceptance tests | Jest/Playwright parity suite: .editorconfig |
| `src/AcceptanceTests/AIAgents/ApplicationChatAgentTests.cs` | Browser/system acceptance tests | Jest/Playwright parity suite: AIAgents/ApplicationChatAgentTests.cs |
| `src/AcceptanceTests/AIAgents/AutoReformatAgentTests.cs` | Browser/system acceptance tests | Jest/Playwright parity suite: AIAgents/AutoReformatAgentTests.cs |
| `src/AcceptanceTests/AIAgents/SaturdayMowSchedulingAgentTests.cs` | Browser/system acceptance tests | Jest/Playwright parity suite: AIAgents/SaturdayMowSchedulingAgentTests.cs |
| `src/AcceptanceTests/AcceptanceTestBase.cs` | Browser/system acceptance tests | Jest/Playwright parity suite: AcceptanceTestBase.cs |
| `src/AcceptanceTests/AcceptanceTests.csproj` | Browser/system acceptance tests | Deferred: replace after equivalent behavior lands; retain only as migration reference |
| `src/AcceptanceTests/AcceptanceTests.runsettings` | Browser/system acceptance tests | Jest/Playwright parity suite: AcceptanceTests.runsettings |
| `src/AcceptanceTests/Api/ApiRateLimitingAcceptanceTests.cs` | Browser/system acceptance tests | Jest/Playwright parity suite: Api/ApiRateLimitingAcceptanceTests.cs |
| `src/AcceptanceTests/Api/DetailedHealthApiAcceptanceTests.cs` | Browser/system acceptance tests | Jest/Playwright parity suite: Api/DetailedHealthApiAcceptanceTests.cs |
| `src/AcceptanceTests/Api/EchoApiAcceptanceTests.cs` | Browser/system acceptance tests | Jest/Playwright parity suite: Api/EchoApiAcceptanceTests.cs |
| `src/AcceptanceTests/Api/FeatureFlagsApiAcceptanceTests.cs` | Browser/system acceptance tests | Jest/Playwright parity suite: Api/FeatureFlagsApiAcceptanceTests.cs |
| `src/AcceptanceTests/Api/MetricsSummaryAcceptanceTests.cs` | Browser/system acceptance tests | Jest/Playwright parity suite: Api/MetricsSummaryAcceptanceTests.cs |
| `src/AcceptanceTests/App/AiAgentPageTests.cs` | Browser/system acceptance tests | Jest/Playwright parity suite: App/AiAgentPageTests.cs |
| `src/AcceptanceTests/App/ClientHealthCheckTests.cs` | Browser/system acceptance tests | Jest/Playwright parity suite: App/ClientHealthCheckTests.cs |
| `src/AcceptanceTests/App/CopyrightFooterTests.cs` | Browser/system acceptance tests | Jest/Playwright parity suite: App/CopyrightFooterTests.cs |
| `src/AcceptanceTests/App/CounterTests.cs` | Browser/system acceptance tests | Jest/Playwright parity suite: App/CounterTests.cs |
| `src/AcceptanceTests/App/DarkModeTests.cs` | Browser/system acceptance tests | Jest/Playwright parity suite: App/DarkModeTests.cs |
| `src/AcceptanceTests/App/LandingPageTests.cs` | Browser/system acceptance tests | Jest/Playwright parity suite: App/LandingPageTests.cs |
| `src/AcceptanceTests/App/NavMenuTests.cs` | Browser/system acceptance tests | Jest/Playwright parity suite: App/NavMenuTests.cs |
| `src/AcceptanceTests/App/NavRailToggleTests.cs` | Browser/system acceptance tests | Jest/Playwright parity suite: App/NavRailToggleTests.cs |
| `src/AcceptanceTests/App/NeedsRebootHealthCheckTests.cs` | Browser/system acceptance tests | Jest/Playwright parity suite: App/NeedsRebootHealthCheckTests.cs |
| `src/AcceptanceTests/App/WarmUpTests.cs` | Browser/system acceptance tests | Jest/Playwright parity suite: App/WarmUpTests.cs |
| `src/AcceptanceTests/Authentication/LoginTests.cs` | Browser/system acceptance tests | Jest/Playwright parity suite: Authentication/LoginTests.cs |
| `src/AcceptanceTests/Authentication/LogoutTests.cs` | Browser/system acceptance tests | Jest/Playwright parity suite: Authentication/LogoutTests.cs |
| `src/AcceptanceTests/BlazorWasmWarmUp.cs` | Browser/system acceptance tests | Jest/Playwright parity suite: BlazorWasmWarmUp.cs |
| `src/AcceptanceTests/Extensions/DateTimeTestExtensions.cs` | Browser/system acceptance tests | Jest/Playwright parity suite: Extensions/DateTimeTestExtensions.cs |
| `src/AcceptanceTests/Extensions/DateTimeTestExtensionsTests.cs` | Browser/system acceptance tests | Jest/Playwright parity suite: Extensions/DateTimeTestExtensionsTests.cs |
| `src/AcceptanceTests/McpServer/McpChatConversationTests.cs` | Browser/system acceptance tests | Jest/Playwright parity suite: McpServer/McpChatConversationTests.cs |
| `src/AcceptanceTests/McpServer/McpCreateWorkOrderInstructionsAcceptanceTests.cs` | Browser/system acceptance tests | Jest/Playwright parity suite: McpServer/McpCreateWorkOrderInstructionsAcceptanceTests.cs |
| `src/AcceptanceTests/McpServer/McpGetWorkOrderInstructionsAcceptanceTests.cs` | Browser/system acceptance tests | Jest/Playwright parity suite: McpServer/McpGetWorkOrderInstructionsAcceptanceTests.cs |
| `src/AcceptanceTests/McpServer/McpHttpServerAcceptanceTests.cs` | Browser/system acceptance tests | Jest/Playwright parity suite: McpServer/McpHttpServerAcceptanceTests.cs |
| `src/AcceptanceTests/McpServer/McpListWorkOrdersAcceptanceTests.cs` | Browser/system acceptance tests | Jest/Playwright parity suite: McpServer/McpListWorkOrdersAcceptanceTests.cs |
| `src/AcceptanceTests/McpServer/McpSaveWorkOrderAcceptanceTests.cs` | Browser/system acceptance tests | Jest/Playwright parity suite: McpServer/McpSaveWorkOrderAcceptanceTests.cs |
| `src/AcceptanceTests/McpServer/McpServerAcceptanceTests.cs` | Browser/system acceptance tests | Jest/Playwright parity suite: McpServer/McpServerAcceptanceTests.cs |
| `src/AcceptanceTests/McpServer/McpServerLlmAcceptanceTests.cs` | Browser/system acceptance tests | Jest/Playwright parity suite: McpServer/McpServerLlmAcceptanceTests.cs |
| `src/AcceptanceTests/McpServer/McpTestHelper.cs` | Browser/system acceptance tests | Jest/Playwright parity suite: McpServer/McpTestHelper.cs |
| `src/AcceptanceTests/McpServer/McpWorkOrderLifecycleLlmTests.cs` | Browser/system acceptance tests | Jest/Playwright parity suite: McpServer/McpWorkOrderLifecycleLlmTests.cs |
| `src/AcceptanceTests/McpServer/McpWorkOrderLifecycleTests.cs` | Browser/system acceptance tests | Jest/Playwright parity suite: McpServer/McpWorkOrderLifecycleTests.cs |
| `src/AcceptanceTests/NServiceBus/TracerBulletTests.cs` | Browser/system acceptance tests | Jest/Playwright parity suite: NServiceBus/TracerBulletTests.cs |
| `src/AcceptanceTests/ProcessCleanupHelper.cs` | Browser/system acceptance tests | Jest/Playwright parity suite: ProcessCleanupHelper.cs |
| `src/AcceptanceTests/Properties/AssemblyInfo.cs` | Browser/system acceptance tests | Jest/Playwright parity suite: Properties/AssemblyInfo.cs |
| `src/AcceptanceTests/ServerFixture.cs` | Browser/system acceptance tests | Jest/Playwright parity suite: ServerFixture.cs |
| `src/AcceptanceTests/TestHttpClientFactory.cs` | Browser/system acceptance tests | Jest/Playwright parity suite: TestHttpClientFactory.cs |
| `src/AcceptanceTests/Usings.cs` | Browser/system acceptance tests | Jest/Playwright parity suite: Usings.cs |
| `src/AcceptanceTests/WorkOrders/WorkOrderAIChatTests.cs` | Browser/system acceptance tests | Jest/Playwright parity suite: WorkOrders/WorkOrderAIChatTests.cs |
| `src/AcceptanceTests/WorkOrders/WorkOrderAssignTests.cs` | Browser/system acceptance tests | Jest/Playwright parity suite: WorkOrders/WorkOrderAssignTests.cs |
| `src/AcceptanceTests/WorkOrders/WorkOrderAttachmentTests.cs` | Browser/system acceptance tests | Jest/Playwright parity suite: WorkOrders/WorkOrderAttachmentTests.cs |
| `src/AcceptanceTests/WorkOrders/WorkOrderBeginTests.cs` | Browser/system acceptance tests | Jest/Playwright parity suite: WorkOrders/WorkOrderBeginTests.cs |
| `src/AcceptanceTests/WorkOrders/WorkOrderCancelTests.cs` | Browser/system acceptance tests | Jest/Playwright parity suite: WorkOrders/WorkOrderCancelTests.cs |
| `src/AcceptanceTests/WorkOrders/WorkOrderCompleteTests.cs` | Browser/system acceptance tests | Jest/Playwright parity suite: WorkOrders/WorkOrderCompleteTests.cs |
| `src/AcceptanceTests/WorkOrders/WorkOrderDescriptionCharCountTests.cs` | Browser/system acceptance tests | Jest/Playwright parity suite: WorkOrders/WorkOrderDescriptionCharCountTests.cs |
| `src/AcceptanceTests/WorkOrders/WorkOrderDictationTests.cs` | Browser/system acceptance tests | Jest/Playwright parity suite: WorkOrders/WorkOrderDictationTests.cs |
| `src/AcceptanceTests/WorkOrders/WorkOrderDueDateTests.cs` | Browser/system acceptance tests | Jest/Playwright parity suite: WorkOrders/WorkOrderDueDateTests.cs |
| `src/AcceptanceTests/WorkOrders/WorkOrderRoomNumberLengthTests.cs` | Browser/system acceptance tests | Jest/Playwright parity suite: WorkOrders/WorkOrderRoomNumberLengthTests.cs |
| `src/AcceptanceTests/WorkOrders/WorkOrderSaveDraftTests.cs` | Browser/system acceptance tests | Jest/Playwright parity suite: WorkOrders/WorkOrderSaveDraftTests.cs |
| `src/AcceptanceTests/WorkOrders/WorkOrderSaveThenAssignPersistenceTests.cs` | Browser/system acceptance tests | Jest/Playwright parity suite: WorkOrders/WorkOrderSaveThenAssignPersistenceTests.cs |
| `src/AcceptanceTests/WorkOrders/WorkOrderSearchTests.cs` | Browser/system acceptance tests | Jest/Playwright parity suite: WorkOrders/WorkOrderSearchTests.cs |
| `src/AcceptanceTests/WorkOrders/WorkOrderShelvedTests.cs` | Browser/system acceptance tests | Jest/Playwright parity suite: WorkOrders/WorkOrderShelvedTests.cs |
| `src/AcceptanceTests/WorkOrders/WorkOrderSpeechTests.cs` | Browser/system acceptance tests | Jest/Playwright parity suite: WorkOrders/WorkOrderSpeechTests.cs |
| `src/AcceptanceTests/WorkOrders/WorkOrderStatusDashboardTests.cs` | Browser/system acceptance tests | Jest/Playwright parity suite: WorkOrders/WorkOrderStatusDashboardTests.cs |
| `src/AcceptanceTests/appsettings.acceptancetests.json` | Browser/system acceptance tests | Jest/Playwright parity suite: appsettings.acceptancetests.json |
| `src/ChurchBulletin.AppHost/AppHost.cs` | Local orchestration | Compose/observability configuration: AppHost.cs |
| `src/ChurchBulletin.AppHost/ChurchBulletin.AppHost.csproj` | Local orchestration | Deferred: replace after equivalent behavior lands; retain only as migration reference |
| `src/ChurchBulletin.AppHost/Properties/launchSettings.json` | Local orchestration | Compose/observability configuration: Properties/launchSettings.json |
| `src/ChurchBulletin.AppHost/appsettings.Development.json` | Local orchestration | Compose/observability configuration: appsettings.Development.json |
| `src/ChurchBulletin.AppHost/appsettings.json` | Local orchestration | Compose/observability configuration: appsettings.json |
| `src/ChurchBulletin.ServiceDefaults/ChurchBulletin.ServiceDefaults.csproj` | Telemetry and service defaults | Deferred: replace after equivalent behavior lands; retain only as migration reference |
| `src/ChurchBulletin.ServiceDefaults/CorrelationIdConstants.cs` | Telemetry and service defaults | Compose/observability configuration: CorrelationIdConstants.cs |
| `src/ChurchBulletin.ServiceDefaults/CorrelationIdMiddleware.cs` | Telemetry and service defaults | Compose/observability configuration: CorrelationIdMiddleware.cs |
| `src/ChurchBulletin.ServiceDefaults/CorrelationIdMiddlewareExtensions.cs` | Telemetry and service defaults | Compose/observability configuration: CorrelationIdMiddlewareExtensions.cs |
| `src/ChurchBulletin.ServiceDefaults/CorrelationIdResolver.cs` | Telemetry and service defaults | Compose/observability configuration: CorrelationIdResolver.cs |
| `src/ChurchBulletin.ServiceDefaults/EventEntry.cs` | Telemetry and service defaults | Compose/observability configuration: EventEntry.cs |
| `src/ChurchBulletin.ServiceDefaults/Extensions.cs` | Telemetry and service defaults | Compose/observability configuration: Extensions.cs |
| `src/ChurchBulletin.ServiceDefaults/InternalsVisibleTo.cs` | Telemetry and service defaults | Compose/observability configuration: InternalsVisibleTo.cs |
| `src/ChurchBulletin.ServiceDefaults/LocalTelemetryFileWriter.cs` | Telemetry and service defaults | Compose/observability configuration: LocalTelemetryFileWriter.cs |
| `src/ChurchBulletin.ServiceDefaults/LocalTelemetryLoggerProvider.cs` | Telemetry and service defaults | Compose/observability configuration: LocalTelemetryLoggerProvider.cs |
| `src/ChurchBulletin.ServiceDefaults/LogEntry.cs` | Telemetry and service defaults | Compose/observability configuration: LogEntry.cs |
| `src/ChurchBulletin.ServiceDefaults/LogEntryError.cs` | Telemetry and service defaults | Compose/observability configuration: LogEntryError.cs |
| `src/ChurchBulletin.ServiceDefaults/MetricEntry.cs` | Telemetry and service defaults | Compose/observability configuration: MetricEntry.cs |
| `src/ChurchBulletin.ServiceDefaults/SerilogExtensions.cs` | Telemetry and service defaults | Compose/observability configuration: SerilogExtensions.cs |
| `src/ChurchBulletin.ServiceDefaults/TelemetryFileMaintenance.cs` | Telemetry and service defaults | Compose/observability configuration: TelemetryFileMaintenance.cs |
| `src/ChurchBulletin.ServiceDefaults/TraceEntry.cs` | Telemetry and service defaults | Compose/observability configuration: TraceEntry.cs |
| `src/ChurchBulletin.ServiceDefaults/TraceEntryMapper.cs` | Telemetry and service defaults | Compose/observability configuration: TraceEntryMapper.cs |
| `src/ChurchBulletin.sln` | ChurchBulletin.sln project | Deferred: replace after equivalent behavior lands; retain only as migration reference |
| `src/ChurchBulletin.sln.DotSettings` | ChurchBulletin.sln.DotSettings project | Review and map from ChurchBulletin.sln.DotSettings:  |
| `src/Core/ConfigurationModel.cs` | Domain and application contracts | Nest domain/application: ConfigurationModel.cs |
| `src/Core/ContainerARM.json` | Domain and application contracts | Nest domain/application: ContainerARM.json |
| `src/Core/ContainerEnvironmentARM.json` | Domain and application contracts | Nest domain/application: ContainerEnvironmentARM.json |
| `src/Core/ContainerEnvironmentARMParameters.json` | Domain and application contracts | Nest domain/application: ContainerEnvironmentARMParameters.json |
| `src/Core/Core.csproj` | Domain and application contracts | Deferred: replace after equivalent behavior lands; retain only as migration reference |
| `src/Core/ExcludeFromBusActivityAttribute.cs` | Domain and application contracts | Nest domain/application: ExcludeFromBusActivityAttribute.cs |
| `src/Core/HealthCheckRemotableRequest.cs` | Domain and application contracts | Nest domain/application: HealthCheckRemotableRequest.cs |
| `src/Core/IBus.cs` | Domain and application contracts | Nest domain/application: IBus.cs |
| `src/Core/IDatabaseConfiguration.cs` | Domain and application contracts | Nest domain/application: IDatabaseConfiguration.cs |
| `src/Core/IDistributedBus.cs` | Domain and application contracts | Nest domain/application: IDistributedBus.cs |
| `src/Core/IRemotableEvent.cs` | Domain and application contracts | Nest domain/application: IRemotableEvent.cs |
| `src/Core/IRemotableRequest.cs` | Domain and application contracts | Nest domain/application: IRemotableRequest.cs |
| `src/Core/Import/WorkOrderBulkImportCsvParser.cs` | Domain and application contracts | Nest domain/application: Import/WorkOrderBulkImportCsvParser.cs |
| `src/Core/Import/WorkOrderBulkImportParseResult.cs` | Domain and application contracts | Nest domain/application: Import/WorkOrderBulkImportParseResult.cs |
| `src/Core/Import/WorkOrderBulkImportRow.cs` | Domain and application contracts | Nest domain/application: Import/WorkOrderBulkImportRow.cs |
| `src/Core/Messaging/WebServiceMessage.cs` | Domain and application contracts | Nest domain/application: Messaging/WebServiceMessage.cs |
| `src/Core/Model/Constants/Roles.cs` | Domain and application contracts | Nest domain/application: Model/Constants/Roles.cs |
| `src/Core/Model/DueDateUrgency.cs` | Domain and application contracts | Nest domain/application: Model/DueDateUrgency.cs |
| `src/Core/Model/Employee.cs` | Domain and application contracts | Nest domain/application: Model/Employee.cs |
| `src/Core/Model/EntityBase.cs` | Domain and application contracts | Nest domain/application: Model/EntityBase.cs |
| `src/Core/Model/Events/DatedWorkOrdersCreatedEvent.cs` | Domain and application contracts | Nest domain/application: Model/Events/DatedWorkOrdersCreatedEvent.cs |
| `src/Core/Model/Events/IStateTransitionEvent.cs` | Domain and application contracts | Nest domain/application: Model/Events/IStateTransitionEvent.cs |
| `src/Core/Model/Events/UserLoggedInEvent.cs` | Domain and application contracts | Nest domain/application: Model/Events/UserLoggedInEvent.cs |
| `src/Core/Model/Events/WorkOrderAssignedToBotEvent.cs` | Domain and application contracts | Nest domain/application: Model/Events/WorkOrderAssignedToBotEvent.cs |
| `src/Core/Model/Messages/TracerBulletCommand.cs` | Domain and application contracts | Nest domain/application: Model/Messages/TracerBulletCommand.cs |
| `src/Core/Model/Messages/TracerBulletReplyMessage.cs` | Domain and application contracts | Nest domain/application: Model/Messages/TracerBulletReplyMessage.cs |
| `src/Core/Model/Role.cs` | Domain and application contracts | Nest domain/application: Model/Role.cs |
| `src/Core/Model/StateCommands/AddAttachmentMetadataCommand.cs` | Domain and application contracts | Nest domain/application: Model/StateCommands/AddAttachmentMetadataCommand.cs |
| `src/Core/Model/StateCommands/AssignedToCancelledCommand.cs` | Domain and application contracts | Nest domain/application: Model/StateCommands/AssignedToCancelledCommand.cs |
| `src/Core/Model/StateCommands/AssignedToInProgressCommand.cs` | Domain and application contracts | Nest domain/application: Model/StateCommands/AssignedToInProgressCommand.cs |
| `src/Core/Model/StateCommands/CreateDatedWorkOrdersCommand.cs` | Domain and application contracts | Nest domain/application: Model/StateCommands/CreateDatedWorkOrdersCommand.cs |
| `src/Core/Model/StateCommands/DraftToAssignedCommand.cs` | Domain and application contracts | Nest domain/application: Model/StateCommands/DraftToAssignedCommand.cs |
| `src/Core/Model/StateCommands/InProgressToAssignedCommand.cs` | Domain and application contracts | Nest domain/application: Model/StateCommands/InProgressToAssignedCommand.cs |
| `src/Core/Model/StateCommands/InProgressToCompleteCommand.cs` | Domain and application contracts | Nest domain/application: Model/StateCommands/InProgressToCompleteCommand.cs |
| `src/Core/Model/StateCommands/SaveDraftCommand.cs` | Domain and application contracts | Nest domain/application: Model/StateCommands/SaveDraftCommand.cs |
| `src/Core/Model/StateCommands/StateCommandBase.cs` | Domain and application contracts | Nest domain/application: Model/StateCommands/StateCommandBase.cs |
| `src/Core/Model/StateCommands/StateCommandResult.cs` | Domain and application contracts | Nest domain/application: Model/StateCommands/StateCommandResult.cs |
| `src/Core/Model/WeatherForecast.cs` | Domain and application contracts | Nest domain/application: Model/WeatherForecast.cs |
| `src/Core/Model/WorkOrder.cs` | Domain and application contracts | Nest domain/application: Model/WorkOrder.cs |
| `src/Core/Model/WorkOrderAttachment.cs` | Domain and application contracts | Nest domain/application: Model/WorkOrderAttachment.cs |
| `src/Core/Model/WorkOrderStatus.cs` | Domain and application contracts | Nest domain/application: Model/WorkOrderStatus.cs |
| `src/Core/Queries/ApplicationChatQuery.cs` | Domain and application contracts | Nest domain/application: Queries/ApplicationChatQuery.cs |
| `src/Core/Queries/EmployeeByUserNameQuery.cs` | Domain and application contracts | Nest domain/application: Queries/EmployeeByUserNameQuery.cs |
| `src/Core/Queries/EmployeeGetAllQuery.cs` | Domain and application contracts | Nest domain/application: Queries/EmployeeGetAllQuery.cs |
| `src/Core/Queries/ForecastQuery.cs` | Domain and application contracts | Nest domain/application: Queries/ForecastQuery.cs |
| `src/Core/Queries/WorkOrderAttachmentsQuery.cs` | Domain and application contracts | Nest domain/application: Queries/WorkOrderAttachmentsQuery.cs |
| `src/Core/Queries/WorkOrderByNumberQuery.cs` | Domain and application contracts | Nest domain/application: Queries/WorkOrderByNumberQuery.cs |
| `src/Core/Queries/WorkOrderCountByStatusQuery.cs` | Domain and application contracts | Nest domain/application: Queries/WorkOrderCountByStatusQuery.cs |
| `src/Core/Queries/WorkOrderSpecificationQuery.cs` | Domain and application contracts | Nest domain/application: Queries/WorkOrderSpecificationQuery.cs |
| `src/Core/Services/ChurchTimeZone.cs` | Domain and application contracts | Nest domain/application: Services/ChurchTimeZone.cs |
| `src/Core/Services/DueDateUrgencyCalculator.cs` | Domain and application contracts | Nest domain/application: Services/DueDateUrgencyCalculator.cs |
| `src/Core/Services/IStateCommand.cs` | Domain and application contracts | Nest domain/application: Services/IStateCommand.cs |
| `src/Core/Services/ITranslationService.cs` | Domain and application contracts | Nest domain/application: Services/ITranslationService.cs |
| `src/Core/Services/IUserSession.cs` | Domain and application contracts | Nest domain/application: Services/IUserSession.cs |
| `src/Core/Services/IWorkOrderBuilder.cs` | Domain and application contracts | Nest domain/application: Services/IWorkOrderBuilder.cs |
| `src/Core/Services/IWorkOrderNumberGenerator.cs` | Domain and application contracts | Nest domain/application: Services/IWorkOrderNumberGenerator.cs |
| `src/Core/Services/Impl/StateCommandList.cs` | Domain and application contracts | Nest domain/application: Services/Impl/StateCommandList.cs |
| `src/Core/Services/Impl/WorkOrderBuilder.cs` | Domain and application contracts | Nest domain/application: Services/Impl/WorkOrderBuilder.cs |
| `src/Core/Services/Impl/WorkOrderNumberGenerator.cs` | Domain and application contracts | Nest domain/application: Services/Impl/WorkOrderNumberGenerator.cs |
| `src/Core/Services/StateCommandContext.cs` | Domain and application contracts | Nest domain/application: Services/StateCommandContext.cs |
| `src/Core/Services/WorkOrderSearchSpecification.cs` | Domain and application contracts | Nest domain/application: Services/WorkOrderSearchSpecification.cs |
| `src/Core/Validation/ApplicationChatQueryValidator.cs` | Domain and application contracts | Nest domain/application: Validation/ApplicationChatQueryValidator.cs |
| `src/Core/Validation/AssignedToCancelledCommandValidator.cs` | Domain and application contracts | Nest domain/application: Validation/AssignedToCancelledCommandValidator.cs |
| `src/Core/Validation/AssignedToInProgressCommandValidator.cs` | Domain and application contracts | Nest domain/application: Validation/AssignedToInProgressCommandValidator.cs |
| `src/Core/Validation/CreateDatedWorkOrdersCommandValidator.cs` | Domain and application contracts | Nest domain/application: Validation/CreateDatedWorkOrdersCommandValidator.cs |
| `src/Core/Validation/DraftToAssignedCommandValidator.cs` | Domain and application contracts | Nest domain/application: Validation/DraftToAssignedCommandValidator.cs |
| `src/Core/Validation/EmployeeByUserNameQueryValidator.cs` | Domain and application contracts | Nest domain/application: Validation/EmployeeByUserNameQueryValidator.cs |
| `src/Core/Validation/ForecastQueryValidator.cs` | Domain and application contracts | Nest domain/application: Validation/ForecastQueryValidator.cs |
| `src/Core/Validation/HealthCheckRemotableRequestValidator.cs` | Domain and application contracts | Nest domain/application: Validation/HealthCheckRemotableRequestValidator.cs |
| `src/Core/Validation/InProgressToAssignedCommandValidator.cs` | Domain and application contracts | Nest domain/application: Validation/InProgressToAssignedCommandValidator.cs |
| `src/Core/Validation/InProgressToCompleteCommandValidator.cs` | Domain and application contracts | Nest domain/application: Validation/InProgressToCompleteCommandValidator.cs |
| `src/Core/Validation/SaveDraftCommandValidator.cs` | Domain and application contracts | Nest domain/application: Validation/SaveDraftCommandValidator.cs |
| `src/Core/Validation/UserLoggedInEventValidator.cs` | Domain and application contracts | Nest domain/application: Validation/UserLoggedInEventValidator.cs |
| `src/Core/Validation/WebServiceMessageValidator.cs` | Domain and application contracts | Nest domain/application: Validation/WebServiceMessageValidator.cs |
| `src/Core/Validation/WorkOrderAttachmentsQueryValidator.cs` | Domain and application contracts | Nest domain/application: Validation/WorkOrderAttachmentsQueryValidator.cs |
| `src/Core/Validation/WorkOrderByNumberQueryValidator.cs` | Domain and application contracts | Nest domain/application: Validation/WorkOrderByNumberQueryValidator.cs |
| `src/Core/Validation/WorkOrderCountByStatusQueryValidator.cs` | Domain and application contracts | Nest domain/application: Validation/WorkOrderCountByStatusQueryValidator.cs |
| `src/Core/Validation/WorkOrderSpecificationQueryValidator.cs` | Domain and application contracts | Nest domain/application: Validation/WorkOrderSpecificationQueryValidator.cs |
| `src/DataAccess/CanConnectToDatabaseHealthCheck.cs` | Persistence and application handlers | Nest persistence/application: CanConnectToDatabaseHealthCheck.cs |
| `src/DataAccess/DataAccess.csproj` | Persistence and application handlers | Deferred: replace after equivalent behavior lands; retain only as migration reference |
| `src/DataAccess/Handlers/AddAttachmentMetadataCommandHandler.cs` | Persistence and application handlers | Nest persistence/application: Handlers/AddAttachmentMetadataCommandHandler.cs |
| `src/DataAccess/Handlers/CreateDatedWorkOrdersHandler.cs` | Persistence and application handlers | Nest persistence/application: Handlers/CreateDatedWorkOrdersHandler.cs |
| `src/DataAccess/Handlers/DatedWorkOrdersCreatedTelemetryHandler.cs` | Persistence and application handlers | Nest persistence/application: Handlers/DatedWorkOrdersCreatedTelemetryHandler.cs |
| `src/DataAccess/Handlers/EmployeeQueryHandler.cs` | Persistence and application handlers | Nest persistence/application: Handlers/EmployeeQueryHandler.cs |
| `src/DataAccess/Handlers/ForecastQueryHandler.cs` | Persistence and application handlers | Nest persistence/application: Handlers/ForecastQueryHandler.cs |
| `src/DataAccess/Handlers/StateCommandHandler.cs` | Persistence and application handlers | Nest persistence/application: Handlers/StateCommandHandler.cs |
| `src/DataAccess/Handlers/TelemetryHandler.cs` | Persistence and application handlers | Nest persistence/application: Handlers/TelemetryHandler.cs |
| `src/DataAccess/Handlers/WorkOrderAttachmentsQueryHandler.cs` | Persistence and application handlers | Nest persistence/application: Handlers/WorkOrderAttachmentsQueryHandler.cs |
| `src/DataAccess/Handlers/WorkOrderCountByStatusQueryHandler.cs` | Persistence and application handlers | Nest persistence/application: Handlers/WorkOrderCountByStatusQueryHandler.cs |
| `src/DataAccess/Handlers/WorkOrderQueryFilters.cs` | Persistence and application handlers | Nest persistence/application: Handlers/WorkOrderQueryFilters.cs |
| `src/DataAccess/Handlers/WorkOrderQueryHandler.cs` | Persistence and application handlers | Nest persistence/application: Handlers/WorkOrderQueryHandler.cs |
| `src/DataAccess/Handlers/WorkOrderSearchHandler.cs` | Persistence and application handlers | Nest persistence/application: Handlers/WorkOrderSearchHandler.cs |
| `src/DataAccess/HealthCheckRemotableRequestHandler.cs` | Persistence and application handlers | Nest persistence/application: HealthCheckRemotableRequestHandler.cs |
| `src/DataAccess/InternalsVisibleTo.cs` | Persistence and application handlers | Nest persistence/application: InternalsVisibleTo.cs |
| `src/DataAccess/Mappings/DataContext.cs` | Persistence and application handlers | Nest persistence/application: Mappings/DataContext.cs |
| `src/DataAccess/Mappings/EmployeeMap.cs` | Persistence and application handlers | Nest persistence/application: Mappings/EmployeeMap.cs |
| `src/DataAccess/Mappings/IEntityFrameworkMapping.cs` | Persistence and application handlers | Nest persistence/application: Mappings/IEntityFrameworkMapping.cs |
| `src/DataAccess/Mappings/RoleMap.cs` | Persistence and application handlers | Nest persistence/application: Mappings/RoleMap.cs |
| `src/DataAccess/Mappings/WorkOrderAttachmentMap.cs` | Persistence and application handlers | Nest persistence/application: Mappings/WorkOrderAttachmentMap.cs |
| `src/DataAccess/Mappings/WorkOrderMap.cs` | Persistence and application handlers | Nest persistence/application: Mappings/WorkOrderMap.cs |
| `src/DataAccess/Mappings/WorkOrderStatusConverter.cs` | Persistence and application handlers | Nest persistence/application: Mappings/WorkOrderStatusConverter.cs |
| `src/DataAccess/Messaging/DistributedBus.cs` | Persistence and application handlers | Nest persistence/application: Messaging/DistributedBus.cs |
| `src/DataAccess/Messaging/MessagingConventions.cs` | Persistence and application handlers | Nest persistence/application: Messaging/MessagingConventions.cs |
| `src/DataAccess/Properties/launchSettings.json` | Persistence and application handlers | Nest persistence/application: Properties/launchSettings.json |
| `src/Database/Console/AbstractDatabaseCommand.cs` | Database schema and deployment | Prisma migrations or deployment config: Console/AbstractDatabaseCommand.cs |
| `src/Database/Console/BaselineDatabaseCommand.cs` | Database schema and deployment | Prisma migrations or deployment config: Console/BaselineDatabaseCommand.cs |
| `src/Database/Console/DatabaseConnectionStringBuilder.cs` | Database schema and deployment | Prisma migrations or deployment config: Console/DatabaseConnectionStringBuilder.cs |
| `src/Database/Console/DatabaseOptions.cs` | Database schema and deployment | Prisma migrations or deployment config: Console/DatabaseOptions.cs |
| `src/Database/Console/DatabaseRebuildSteps.cs` | Database schema and deployment | Prisma migrations or deployment config: Console/DatabaseRebuildSteps.cs |
| `src/Database/Console/DatabaseUpgradeLogSelector.cs` | Database schema and deployment | Prisma migrations or deployment config: Console/DatabaseUpgradeLogSelector.cs |
| `src/Database/Console/NullUpgradeLog.cs` | Database schema and deployment | Prisma migrations or deployment config: Console/NullUpgradeLog.cs |
| `src/Database/Console/QuietLog.cs` | Database schema and deployment | Prisma migrations or deployment config: Console/QuietLog.cs |
| `src/Database/Console/RebuildDatabaseCommand.cs` | Database schema and deployment | Prisma migrations or deployment config: Console/RebuildDatabaseCommand.cs |
| `src/Database/Console/ScriptBaselineMarker.cs` | Database schema and deployment | Prisma migrations or deployment config: Console/ScriptBaselineMarker.cs |
| `src/Database/Console/UpdateDatabaseCommand.cs` | Database schema and deployment | Prisma migrations or deployment config: Console/UpdateDatabaseCommand.cs |
| `src/Database/Database.csproj` | Database schema and deployment | Deferred: replace after equivalent behavior lands; retain only as migration reference |
| `src/Database/DatabaseARM.json` | Database schema and deployment | Prisma migrations or deployment config: DatabaseARM.json |
| `src/Database/DatabaseARMParameters.json` | Database schema and deployment | Prisma migrations or deployment config: DatabaseARMParameters.json |
| `src/Database/Program.cs` | Database schema and deployment | Prisma migrations or deployment config: Program.cs |
| `src/Database/scripts/Create/placeholder.txt` | Database schema and deployment | Prisma migrations or deployment config: scripts/Create/placeholder.txt |
| `src/Database/scripts/Everytime/placeholder.txt` | Database schema and deployment | Prisma migrations or deployment config: scripts/Everytime/placeholder.txt |
| `src/Database/scripts/TestData/placeholder.txt` | Database schema and deployment | Prisma migrations or deployment config: scripts/TestData/placeholder.txt |
| `src/Database/scripts/Update/001-first-script.sql` | Database schema and deployment | Prisma migrations or deployment config: scripts/Update/001-first-script.sql |
| `src/Database/scripts/Update/003-WorkOrderAndEmployeeTables.sql` | Database schema and deployment | Prisma migrations or deployment config: scripts/Update/003-WorkOrderAndEmployeeTables.sql |
| `src/Database/scripts/Update/004_RenameConstraintsToGoodNames.sql` | Database schema and deployment | Prisma migrations or deployment config: scripts/Update/004_RenameConstraintsToGoodNames.sql |
| `src/Database/scripts/Update/005_AddedWorkOrderNumber.sql` | Database schema and deployment | Prisma migrations or deployment config: scripts/Update/005_AddedWorkOrderNumber.sql |
| `src/Database/scripts/Update/006_MakeAssigneeNullable.sql` | Database schema and deployment | Prisma migrations or deployment config: scripts/Update/006_MakeAssigneeNullable.sql |
| `src/Database/scripts/Update/007_ChangeStatusToChar.sql` | Database schema and deployment | Prisma migrations or deployment config: scripts/Update/007_ChangeStatusToChar.sql |
| `src/Database/scripts/Update/008_AddSomeEmployeeRecords.sql` | Database schema and deployment | Prisma migrations or deployment config: scripts/Update/008_AddSomeEmployeeRecords.sql |
| `src/Database/scripts/Update/009_AddAuditEntry.sql` | Database schema and deployment | Prisma migrations or deployment config: scripts/Update/009_AddAuditEntry.sql |
| `src/Database/scripts/Update/010_AddRole.sql` | Database schema and deployment | Prisma migrations or deployment config: scripts/Update/010_AddRole.sql |
| `src/Database/scripts/Update/011_EmployeeRolesTable.sql` | Database schema and deployment | Prisma migrations or deployment config: scripts/Update/011_EmployeeRolesTable.sql |
| `src/Database/scripts/Update/012_ManagerSecretary.sql` | Database schema and deployment | Prisma migrations or deployment config: scripts/Update/012_ManagerSecretary.sql |
| `src/Database/scripts/Update/013_SecretaryColumn.sql` | Database schema and deployment | Prisma migrations or deployment config: scripts/Update/013_SecretaryColumn.sql |
| `src/Database/scripts/Update/014_MergingEmployees.sql` | Database schema and deployment | Prisma migrations or deployment config: scripts/Update/014_MergingEmployees.sql |
| `src/Database/scripts/Update/015_RemovedAuditEntriesSecretaryManager.sql` | Database schema and deployment | Prisma migrations or deployment config: scripts/Update/015_RemovedAuditEntriesSecretaryManager.sql |
| `src/Database/scripts/Update/016_AddRoomNumberToWorkOrder.sql` | Database schema and deployment | Prisma migrations or deployment config: scripts/Update/016_AddRoomNumberToWorkOrder.sql |
| `src/Database/scripts/Update/017_AddAuditEntryTable.sql` | Database schema and deployment | Prisma migrations or deployment config: scripts/Update/017_AddAuditEntryTable.sql |
| `src/Database/scripts/Update/017_AddCreatedDateAndCompletedDateToWorkOrder.sql` | Database schema and deployment | Prisma migrations or deployment config: scripts/Update/017_AddCreatedDateAndCompletedDateToWorkOrder.sql |
| `src/Database/scripts/Update/018_AddAssignedDateToWorkOrder.sql` | Database schema and deployment | Prisma migrations or deployment config: scripts/Update/018_AddAssignedDateToWorkOrder.sql |
| `src/Database/scripts/Update/019_ChangeRoomNumberFromIntToString.sql` | Database schema and deployment | Prisma migrations or deployment config: scripts/Update/019_ChangeRoomNumberFromIntToString.sql |
| `src/Database/scripts/Update/020_DropAuditEntry.sql` | Database schema and deployment | Prisma migrations or deployment config: scripts/Update/020_DropAuditEntry.sql |
| `src/Database/scripts/Update/021_ExtendWorkOrderNumberLength.sql` | Database schema and deployment | Prisma migrations or deployment config: scripts/Update/021_ExtendWorkOrderNumberLength.sql |
| `src/Database/scripts/Update/022_CreateNServiceBusSchema.sql` | Database schema and deployment | Prisma migrations or deployment config: scripts/Update/022_CreateNServiceBusSchema.sql |
| `src/Database/scripts/Update/023_MigrateCancelledWorkOrders.sql` | Database schema and deployment | Prisma migrations or deployment config: scripts/Update/023_MigrateCancelledWorkOrders.sql |
| `src/Database/scripts/Update/024_ExtendWorkOrderTitleLength.sql` | Database schema and deployment | Prisma migrations or deployment config: scripts/Update/024_ExtendWorkOrderTitleLength.sql |
| `src/Database/scripts/Update/025_AddWorkOrderAttachmentTable.sql` | Database schema and deployment | Prisma migrations or deployment config: scripts/Update/025_AddWorkOrderAttachmentTable.sql |
| `src/Database/scripts/Update/026_AddPreferredLanguageToEmployee.sql` | Database schema and deployment | Prisma migrations or deployment config: scripts/Update/026_AddPreferredLanguageToEmployee.sql |
| `src/Database/scripts/Update/027_UpdateWorkOrderDFTDRT.sql` | Database schema and deployment | Prisma migrations or deployment config: scripts/Update/027_UpdateWorkOrderDFTDRT.sql |
| `src/Database/scripts/Update/028_AddInstructionsToWorkOrder.sql` | Database schema and deployment | Prisma migrations or deployment config: scripts/Update/028_AddInstructionsToWorkOrder.sql |
| `src/Database/scripts/Update/029_ExtendWorkOrderRoomNumberLength.sql` | Database schema and deployment | Prisma migrations or deployment config: scripts/Update/029_ExtendWorkOrderRoomNumberLength.sql |
| `src/Database/scripts/Update/030_AddDueDateToWorkOrder.sql` | Database schema and deployment | Prisma migrations or deployment config: scripts/Update/030_AddDueDateToWorkOrder.sql |
| `src/Database/scripts/Update/031_ExtendEmployeeLastNameLength.sql` | Database schema and deployment | Prisma migrations or deployment config: scripts/Update/031_ExtendEmployeeLastNameLength.sql |
| `src/Database/scripts/Update/placeholder.txt` | Database schema and deployment | Prisma migrations or deployment config: scripts/Update/placeholder.txt |
| `src/Database/scripts/UpdateAzurePipelineSql.ps1` | Database schema and deployment | Prisma migrations or deployment config: scripts/UpdateAzurePipelineSql.ps1 |
| `src/Database/scripts/UpdateAzureSql.ps1` | Database schema and deployment | Prisma migrations or deployment config: scripts/UpdateAzureSql.ps1 |
| `src/IntegrationTests/.editorconfig` | Integration tests | Jest/Playwright parity suite: .editorconfig |
| `src/IntegrationTests/Api/DetailedHealthCheckEndpointIntegrationTests.cs` | Integration tests | Jest/Playwright parity suite: Api/DetailedHealthCheckEndpointIntegrationTests.cs |
| `src/IntegrationTests/Api/DetailedHealthEndpointIntegrationTests.cs` | Integration tests | Jest/Playwright parity suite: Api/DetailedHealthEndpointIntegrationTests.cs |
| `src/IntegrationTests/Api/DetailedHealthWebApplicationFactory.cs` | Integration tests | Jest/Playwright parity suite: Api/DetailedHealthWebApplicationFactory.cs |
| `src/IntegrationTests/Api/DiagnosticsApiKeyProtectedWebApplicationFactory.cs` | Integration tests | Jest/Playwright parity suite: Api/DiagnosticsApiKeyProtectedWebApplicationFactory.cs |
| `src/IntegrationTests/Api/DiagnosticsEndpointIntegrationTests.cs` | Integration tests | Jest/Playwright parity suite: Api/DiagnosticsEndpointIntegrationTests.cs |
| `src/IntegrationTests/Api/DiagnosticsWebApplicationFactory.cs` | Integration tests | Jest/Playwright parity suite: Api/DiagnosticsWebApplicationFactory.cs |
| `src/IntegrationTests/Api/EchoEndpointIntegrationTests.cs` | Integration tests | Jest/Playwright parity suite: Api/EchoEndpointIntegrationTests.cs |
| `src/IntegrationTests/Api/EnvironmentStatusEndpointIntegrationTests.cs` | Integration tests | Jest/Playwright parity suite: Api/EnvironmentStatusEndpointIntegrationTests.cs |
| `src/IntegrationTests/Api/FeatureFlagsEndpointIntegrationTests.cs` | Integration tests | Jest/Playwright parity suite: Api/FeatureFlagsEndpointIntegrationTests.cs |
| `src/IntegrationTests/Api/GrpcWebApplicationFactory.cs` | Integration tests | Jest/Playwright parity suite: Api/GrpcWebApplicationFactory.cs |
| `src/IntegrationTests/Api/GrpcWorkOrderIntegrationTests.cs` | Integration tests | Jest/Playwright parity suite: Api/GrpcWorkOrderIntegrationTests.cs |
| `src/IntegrationTests/Api/MetricsSummaryEndpointIntegrationTests.cs` | Integration tests | Jest/Playwright parity suite: Api/MetricsSummaryEndpointIntegrationTests.cs |
| `src/IntegrationTests/Api/PingEndpointIntegrationTests.cs` | Integration tests | Jest/Playwright parity suite: Api/PingEndpointIntegrationTests.cs |
| `src/IntegrationTests/Api/RealtimeNotificationWebSocketIntegrationTests.cs` | Integration tests | Jest/Playwright parity suite: Api/RealtimeNotificationWebSocketIntegrationTests.cs |
| `src/IntegrationTests/Api/TimestampConverterEndpointIntegrationTests.cs` | Integration tests | Jest/Playwright parity suite: Api/TimestampConverterEndpointIntegrationTests.cs |
| `src/IntegrationTests/Api/ToolsDueDateCheckEndpointIntegrationTests.cs` | Integration tests | Jest/Playwright parity suite: Api/ToolsDueDateCheckEndpointIntegrationTests.cs |
| `src/IntegrationTests/Api/ToolsGuidGeneratorEndpointIntegrationTests.cs` | Integration tests | Jest/Playwright parity suite: Api/ToolsGuidGeneratorEndpointIntegrationTests.cs |
| `src/IntegrationTests/Api/ToolsHashEndpointIntegrationTests.cs` | Integration tests | Jest/Playwright parity suite: Api/ToolsHashEndpointIntegrationTests.cs |
| `src/IntegrationTests/Api/ToolsRandomEndpointIntegrationTests.cs` | Integration tests | Jest/Playwright parity suite: Api/ToolsRandomEndpointIntegrationTests.cs |
| `src/IntegrationTests/Api/ToolsWordCountEndpointIntegrationTests.cs` | Integration tests | Jest/Playwright parity suite: Api/ToolsWordCountEndpointIntegrationTests.cs |
| `src/IntegrationTests/Api/ToolsWorkOrderStatusesEndpointIntegrationTests.cs` | Integration tests | Jest/Playwright parity suite: Api/ToolsWorkOrderStatusesEndpointIntegrationTests.cs |
| `src/IntegrationTests/Api/VersionEndpointIntegrationTests.cs` | Integration tests | Jest/Playwright parity suite: Api/VersionEndpointIntegrationTests.cs |
| `src/IntegrationTests/Api/WorkOrdersBulkImportIntegrationTests.cs` | Integration tests | Jest/Playwright parity suite: Api/WorkOrdersBulkImportIntegrationTests.cs |
| `src/IntegrationTests/Api/WorkOrdersBulkImportWebApplicationFactory.cs` | Integration tests | Jest/Playwright parity suite: Api/WorkOrdersBulkImportWebApplicationFactory.cs |
| `src/IntegrationTests/BuildGates/CrapGateScriptTests.cs` | Integration tests | Jest/Playwright parity suite: BuildGates/CrapGateScriptTests.cs |
| `src/IntegrationTests/BuildGates/Fixtures/cobertura-async-state-machine.xml` | Integration tests | Jest/Playwright parity suite: BuildGates/Fixtures/cobertura-async-state-machine.xml |
| `src/IntegrationTests/BuildGates/Fixtures/cobertura-orphan-async-state-machine.xml` | Integration tests | Jest/Playwright parity suite: BuildGates/Fixtures/cobertura-orphan-async-state-machine.xml |
| `src/IntegrationTests/BuildGates/Fixtures/crap-production-violations-fail.json` | Integration tests | Jest/Playwright parity suite: BuildGates/Fixtures/crap-production-violations-fail.json |
| `src/IntegrationTests/BuildGates/Fixtures/crap-production-violations-inconsistent.json` | Integration tests | Jest/Playwright parity suite: BuildGates/Fixtures/crap-production-violations-inconsistent.json |
| `src/IntegrationTests/BuildGates/Fixtures/crap-production-violations-malformed.json` | Integration tests | Jest/Playwright parity suite: BuildGates/Fixtures/crap-production-violations-malformed.json |
| `src/IntegrationTests/BuildGates/Fixtures/crap-production-violations-negative.json` | Integration tests | Jest/Playwright parity suite: BuildGates/Fixtures/crap-production-violations-negative.json |
| `src/IntegrationTests/BuildGates/Fixtures/crap-production-violations-pass.json` | Integration tests | Jest/Playwright parity suite: BuildGates/Fixtures/crap-production-violations-pass.json |
| `src/IntegrationTests/DataAccess/DatabaseTests.cs` | Integration tests | Jest/Playwright parity suite: DataAccess/DatabaseTests.cs |
| `src/IntegrationTests/DataAccess/EmployeeQueryHandlerTests.cs` | Integration tests | Jest/Playwright parity suite: DataAccess/EmployeeQueryHandlerTests.cs |
| `src/IntegrationTests/DataAccess/Handlers/CreateDatedWorkOrdersHandlerTests.cs` | Integration tests | Jest/Playwright parity suite: DataAccess/Handlers/CreateDatedWorkOrdersHandlerTests.cs |
| `src/IntegrationTests/DataAccess/Handlers/ForecastQueryHandlerTests.cs` | Integration tests | Jest/Playwright parity suite: DataAccess/Handlers/ForecastQueryHandlerTests.cs |
| `src/IntegrationTests/DataAccess/Handlers/HealthCheckRemotableRequestHandlerTests.cs` | Integration tests | Jest/Playwright parity suite: DataAccess/Handlers/HealthCheckRemotableRequestHandlerTests.cs |
| `src/IntegrationTests/DataAccess/Handlers/StateCommandHandlerForAssignTests.cs` | Integration tests | Jest/Playwright parity suite: DataAccess/Handlers/StateCommandHandlerForAssignTests.cs |
| `src/IntegrationTests/DataAccess/Handlers/StateCommandHandlerForBeginTests.cs` | Integration tests | Jest/Playwright parity suite: DataAccess/Handlers/StateCommandHandlerForBeginTests.cs |
| `src/IntegrationTests/DataAccess/Handlers/StateCommandHandlerForCancelTests.cs` | Integration tests | Jest/Playwright parity suite: DataAccess/Handlers/StateCommandHandlerForCancelTests.cs |
| `src/IntegrationTests/DataAccess/Handlers/StateCommandHandlerForCompleteTests.cs` | Integration tests | Jest/Playwright parity suite: DataAccess/Handlers/StateCommandHandlerForCompleteTests.cs |
| `src/IntegrationTests/DataAccess/Handlers/StateCommandHandlerForSaveTests.cs` | Integration tests | Jest/Playwright parity suite: DataAccess/Handlers/StateCommandHandlerForSaveTests.cs |
| `src/IntegrationTests/DataAccess/Handlers/StateCommandHandlerSaveThenAssignPersistenceTests.cs` | Integration tests | Jest/Playwright parity suite: DataAccess/Handlers/StateCommandHandlerSaveThenAssignPersistenceTests.cs |
| `src/IntegrationTests/DataAccess/Handlers/TelemetryHandlerTests.cs` | Integration tests | Jest/Playwright parity suite: DataAccess/Handlers/TelemetryHandlerTests.cs |
| `src/IntegrationTests/DataAccess/Handlers/WorkOrderCountByStatusQueryHandlerTests.cs` | Integration tests | Jest/Playwright parity suite: DataAccess/Handlers/WorkOrderCountByStatusQueryHandlerTests.cs |
| `src/IntegrationTests/DataAccess/Mappings/EmployeeMappingTests.cs` | Integration tests | Jest/Playwright parity suite: DataAccess/Mappings/EmployeeMappingTests.cs |
| `src/IntegrationTests/DataAccess/Mappings/RoleMappingTests.cs` | Integration tests | Jest/Playwright parity suite: DataAccess/Mappings/RoleMappingTests.cs |
| `src/IntegrationTests/DataAccess/Mappings/WorkOrderMappingTests.cs` | Integration tests | Jest/Playwright parity suite: DataAccess/Mappings/WorkOrderMappingTests.cs |
| `src/IntegrationTests/DataAccess/SqlLoggingTests.cs` | Integration tests | Jest/Playwright parity suite: DataAccess/SqlLoggingTests.cs |
| `src/IntegrationTests/DataAccess/WorkOrderAttachmentHandlerTests.cs` | Integration tests | Jest/Playwright parity suite: DataAccess/WorkOrderAttachmentHandlerTests.cs |
| `src/IntegrationTests/DataAccess/WorkOrderQueryFiltersTests.cs` | Integration tests | Jest/Playwright parity suite: DataAccess/WorkOrderQueryFiltersTests.cs |
| `src/IntegrationTests/DataAccess/WorkOrderQueryHandlerTests.cs` | Integration tests | Jest/Playwright parity suite: DataAccess/WorkOrderQueryHandlerTests.cs |
| `src/IntegrationTests/DataAccess/WorkOrderSpecificationHandlerTests.cs` | Integration tests | Jest/Playwright parity suite: DataAccess/WorkOrderSpecificationHandlerTests.cs |
| `src/IntegrationTests/DatabaseEmptier.cs` | Integration tests | Jest/Playwright parity suite: DatabaseEmptier.cs |
| `src/IntegrationTests/Handlers/TracerBulletReplyHandler.cs` | Integration tests | Jest/Playwright parity suite: Handlers/TracerBulletReplyHandler.cs |
| `src/IntegrationTests/IntegratedTestBase.cs` | Integration tests | Jest/Playwright parity suite: IntegratedTestBase.cs |
| `src/IntegrationTests/IntegrationTests.csproj` | Integration tests | Deferred: replace after equivalent behavior lands; retain only as migration reference |
| `src/IntegrationTests/LlmGateway/ApplicationChatHandlerTests.cs` | Integration tests | Jest/Playwright parity suite: LlmGateway/ApplicationChatHandlerTests.cs |
| `src/IntegrationTests/LlmGateway/CanConnectToLlmServerHealthCheckTests.cs` | Integration tests | Jest/Playwright parity suite: LlmGateway/CanConnectToLlmServerHealthCheckTests.cs |
| `src/IntegrationTests/LlmGateway/LlmTestBase.cs` | Integration tests | Jest/Playwright parity suite: LlmGateway/LlmTestBase.cs |
| `src/IntegrationTests/LlmGateway/TranslationServiceTests.cs` | Integration tests | Jest/Playwright parity suite: LlmGateway/TranslationServiceTests.cs |
| `src/IntegrationTests/LlmGateway/WorkOrderChatHandlerTests.cs` | Integration tests | Jest/Playwright parity suite: LlmGateway/WorkOrderChatHandlerTests.cs |
| `src/IntegrationTests/McpServer/McpCreateDatedWorkOrdersToolTests.cs` | Integration tests | Jest/Playwright parity suite: McpServer/McpCreateDatedWorkOrdersToolTests.cs |
| `src/IntegrationTests/McpServer/McpCreateWorkOrderInstructionsTests.cs` | Integration tests | Jest/Playwright parity suite: McpServer/McpCreateWorkOrderInstructionsTests.cs |
| `src/IntegrationTests/McpServer/McpEmployeeToolTests.cs` | Integration tests | Jest/Playwright parity suite: McpServer/McpEmployeeToolTests.cs |
| `src/IntegrationTests/McpServer/McpEndpointResolverTests.cs` | Integration tests | Jest/Playwright parity suite: McpServer/McpEndpointResolverTests.cs |
| `src/IntegrationTests/McpServer/McpLoopbackHttpClientTests.cs` | Integration tests | Jest/Playwright parity suite: McpServer/McpLoopbackHttpClientTests.cs |
| `src/IntegrationTests/McpServer/McpReferenceResourceTests.cs` | Integration tests | Jest/Playwright parity suite: McpServer/McpReferenceResourceTests.cs |
| `src/IntegrationTests/McpServer/McpSaveWorkOrderTests.cs` | Integration tests | Jest/Playwright parity suite: McpServer/McpSaveWorkOrderTests.cs |
| `src/IntegrationTests/McpServer/McpServerApplicationTests.cs` | Integration tests | Jest/Playwright parity suite: McpServer/McpServerApplicationTests.cs |
| `src/IntegrationTests/McpServer/McpWorkOrderToolTests.cs` | Integration tests | Jest/Playwright parity suite: McpServer/McpWorkOrderToolTests.cs |
| `src/IntegrationTests/McpServer/ToolProviderTests.cs` | Integration tests | Jest/Playwright parity suite: McpServer/ToolProviderTests.cs |
| `src/IntegrationTests/SqlExecuter.cs` | Integration tests | Jest/Playwright parity suite: SqlExecuter.cs |
| `src/IntegrationTests/SqlServerTestAssumptions.cs` | Integration tests | Jest/Playwright parity suite: SqlServerTestAssumptions.cs |
| `src/IntegrationTests/SqliteDatabaseSetup.cs` | Integration tests | Jest/Playwright parity suite: SqliteDatabaseSetup.cs |
| `src/IntegrationTests/TestDatabaseConfiguration.cs` | Integration tests | Jest/Playwright parity suite: TestDatabaseConfiguration.cs |
| `src/IntegrationTests/TestHost.cs` | Integration tests | Jest/Playwright parity suite: TestHost.cs |
| `src/IntegrationTests/TestHostConfigurationTests.cs` | Integration tests | Jest/Playwright parity suite: TestHostConfigurationTests.cs |
| `src/IntegrationTests/TestSetup.cs` | Integration tests | Jest/Playwright parity suite: TestSetup.cs |
| `src/IntegrationTests/TestSupport/LlmTestAttribute.cs` | Integration tests | Jest/Playwright parity suite: TestSupport/LlmTestAttribute.cs |
| `src/IntegrationTests/TestSupport/LlmTestAttributeTests.cs` | Integration tests | Jest/Playwright parity suite: TestSupport/LlmTestAttributeTests.cs |
| `src/IntegrationTests/TracerBulletSignal.cs` | Integration tests | Jest/Playwright parity suite: TracerBulletSignal.cs |
| `src/IntegrationTests/UI/Server/ServerApplicationTests.cs` | Integration tests | Jest/Playwright parity suite: UI/Server/ServerApplicationTests.cs |
| `src/IntegrationTests/Usings.cs` | Integration tests | Jest/Playwright parity suite: Usings.cs |
| `src/IntegrationTests/Worker/WorkerRemotableBusIntegrationTests.cs` | Integration tests | Jest/Playwright parity suite: Worker/WorkerRemotableBusIntegrationTests.cs |
| `src/IntegrationTests/ZDataLoader.cs` | Integration tests | Jest/Playwright parity suite: ZDataLoader.cs |
| `src/IntegrationTests/appsettings.test.json` | Integration tests | Jest/Playwright parity suite: appsettings.test.json |
| `src/LlmGateway/ApplicationChatHandler.cs` | AI integration | Separate TypeScript integration service: ApplicationChatHandler.cs |
| `src/LlmGateway/CanConnectToLlmServerHealthCheck.cs` | AI integration | Separate TypeScript integration service: CanConnectToLlmServerHealthCheck.cs |
| `src/LlmGateway/ChatActivityTracing.cs` | AI integration | Separate TypeScript integration service: ChatActivityTracing.cs |
| `src/LlmGateway/ChatClientAvailabilityResult.cs` | AI integration | Separate TypeScript integration service: ChatClientAvailabilityResult.cs |
| `src/LlmGateway/ChatClientConfig.cs` | AI integration | Separate TypeScript integration service: ChatClientConfig.cs |
| `src/LlmGateway/ChatClientConfigQuery.cs` | AI integration | Separate TypeScript integration service: ChatClientConfigQuery.cs |
| `src/LlmGateway/ChatClientConfigValidator.cs` | AI integration | Separate TypeScript integration service: ChatClientConfigValidator.cs |
| `src/LlmGateway/ChatClientFactory.cs` | AI integration | Separate TypeScript integration service: ChatClientFactory.cs |
| `src/LlmGateway/IToolProvider.cs` | AI integration | Separate TypeScript integration service: IToolProvider.cs |
| `src/LlmGateway/InternalsVisibleTo.cs` | AI integration | Separate TypeScript integration service: InternalsVisibleTo.cs |
| `src/LlmGateway/LlmGateway.csproj` | AI integration | Deferred: replace after equivalent behavior lands; retain only as migration reference |
| `src/LlmGateway/LlmHealthCheckCache.cs` | AI integration | Separate TypeScript integration service: LlmHealthCheckCache.cs |
| `src/LlmGateway/LlmHealthCheckOptions.cs` | AI integration | Separate TypeScript integration service: LlmHealthCheckOptions.cs |
| `src/LlmGateway/LlmHealthEvaluator.cs` | AI integration | Separate TypeScript integration service: LlmHealthEvaluator.cs |
| `src/LlmGateway/TracingChatClient.cs` | AI integration | Separate TypeScript integration service: TracingChatClient.cs |
| `src/LlmGateway/TranslationGuard.cs` | AI integration | Separate TypeScript integration service: TranslationGuard.cs |
| `src/LlmGateway/TranslationService.cs` | AI integration | Separate TypeScript integration service: TranslationService.cs |
| `src/LlmGateway/WorkOrderChatHandler.cs` | AI integration | Separate TypeScript integration service: WorkOrderChatHandler.cs |
| `src/LlmGateway/WorkOrderChatQuery.cs` | AI integration | Separate TypeScript integration service: WorkOrderChatQuery.cs |
| `src/LlmGateway/WorkOrderTool.cs` | AI integration | Separate TypeScript integration service: WorkOrderTool.cs |
| `src/McpServer/DatabaseConfiguration.cs` | MCP integration | Separate TypeScript integration service: DatabaseConfiguration.cs |
| `src/McpServer/McpEndpointResolver.cs` | MCP integration | Separate TypeScript integration service: McpEndpointResolver.cs |
| `src/McpServer/McpLoopbackHttpClient.cs` | MCP integration | Separate TypeScript integration service: McpLoopbackHttpClient.cs |
| `src/McpServer/McpServer.csproj` | MCP integration | Deferred: replace after equivalent behavior lands; retain only as migration reference |
| `src/McpServer/McpServerApplication.cs` | MCP integration | Separate TypeScript integration service: McpServerApplication.cs |
| `src/McpServer/McpServiceRegistry.cs` | MCP integration | Separate TypeScript integration service: McpServiceRegistry.cs |
| `src/McpServer/NullDistributedBus.cs` | MCP integration | Separate TypeScript integration service: NullDistributedBus.cs |
| `src/McpServer/Program.cs` | MCP integration | Separate TypeScript integration service: Program.cs |
| `src/McpServer/Properties/launchSettings.json` | MCP integration | Separate TypeScript integration service: Properties/launchSettings.json |
| `src/McpServer/Resources/ReferenceResources.cs` | MCP integration | Separate TypeScript integration service: Resources/ReferenceResources.cs |
| `src/McpServer/ToolProvider.cs` | MCP integration | Separate TypeScript integration service: ToolProvider.cs |
| `src/McpServer/Tools/DatedWorkOrderScheduling.cs` | MCP integration | Separate TypeScript integration service: Tools/DatedWorkOrderScheduling.cs |
| `src/McpServer/Tools/EmployeeTools.cs` | MCP integration | Separate TypeScript integration service: Tools/EmployeeTools.cs |
| `src/McpServer/Tools/WorkOrderCommandExecutor.cs` | MCP integration | Separate TypeScript integration service: Tools/WorkOrderCommandExecutor.cs |
| `src/McpServer/Tools/WorkOrderTools.cs` | MCP integration | Separate TypeScript integration service: Tools/WorkOrderTools.cs |
| `src/McpServer/appsettings.json` | MCP integration | Separate TypeScript integration service: appsettings.json |
| `src/UI.Shared/ApiHttpContracts.cs` | Shared browser UI | Nest API or React UI (split by responsibility): ApiHttpContracts.cs |
| `src/UI.Shared/AppComponentBase.cs` | Shared browser UI | Nest API or React UI (split by responsibility): AppComponentBase.cs |
| `src/UI.Shared/App_Code/WorkOrderSelectedEvent.cs` | Shared browser UI | Nest API or React UI (split by responsibility): App_Code/WorkOrderSelectedEvent.cs |
| `src/UI.Shared/Authentication/CustomAuthenticationStateProvider.cs` | Shared browser UI | Nest API or React UI (split by responsibility): Authentication/CustomAuthenticationStateProvider.cs |
| `src/UI.Shared/Authentication/IUserSessionStore.cs` | Shared browser UI | Nest API or React UI (split by responsibility): Authentication/IUserSessionStore.cs |
| `src/UI.Shared/Authentication/LocalStorageUserSessionStore.cs` | Shared browser UI | Nest API or React UI (split by responsibility): Authentication/LocalStorageUserSessionStore.cs |
| `src/UI.Shared/Authentication/RedirectToLogin.razor` | Shared browser UI | Nest API or React UI (split by responsibility): Authentication/RedirectToLogin.razor |
| `src/UI.Shared/Bus.cs` | Shared browser UI | Nest API or React UI (split by responsibility): Bus.cs |
| `src/UI.Shared/BusActivityTagger.cs` | Shared browser UI | Nest API or React UI (split by responsibility): BusActivityTagger.cs |
| `src/UI.Shared/Components/HealthCheckLink.razor` | Shared browser UI | Nest API or React UI (split by responsibility): Components/HealthCheckLink.razor |
| `src/UI.Shared/Components/LastWorkOrder.razor` | Shared browser UI | Nest API or React UI (split by responsibility): Components/LastWorkOrder.razor |
| `src/UI.Shared/Components/LoginLink.razor` | Shared browser UI | Nest API or React UI (split by responsibility): Components/LoginLink.razor |
| `src/UI.Shared/Components/LoginLink.razor.css` | Shared browser UI | Nest API or React UI (split by responsibility): Components/LoginLink.razor.css |
| `src/UI.Shared/Components/Logout.razor` | Shared browser UI | Nest API or React UI (split by responsibility): Components/Logout.razor |
| `src/UI.Shared/Components/MyWorkOrders.razor` | Shared browser UI | Nest API or React UI (split by responsibility): Components/MyWorkOrders.razor |
| `src/UI.Shared/Components/WorkOrderChat.razor` | Shared browser UI | Nest API or React UI (split by responsibility): Components/WorkOrderChat.razor |
| `src/UI.Shared/FunJeffreyCustomEventHealthCheck.cs` | Shared browser UI | Nest API or React UI (split by responsibility): FunJeffreyCustomEventHealthCheck.cs |
| `src/UI.Shared/IdempotencyConstants.cs` | Shared browser UI | Nest API or React UI (split by responsibility): IdempotencyConstants.cs |
| `src/UI.Shared/InternalsVisibleTo.cs` | Shared browser UI | Nest API or React UI (split by responsibility): InternalsVisibleTo.cs |
| `src/UI.Shared/LoginDisplayNameFormatter.cs` | Shared browser UI | Nest API or React UI (split by responsibility): LoginDisplayNameFormatter.cs |
| `src/UI.Shared/MainLayout.razor` | Shared browser UI | Nest API or React UI (split by responsibility): MainLayout.razor |
| `src/UI.Shared/MainLayout.razor.cs` | Shared browser UI | Nest API or React UI (split by responsibility): MainLayout.razor.cs |
| `src/UI.Shared/MainLayout.razor.css` | Shared browser UI | Nest API or React UI (split by responsibility): MainLayout.razor.css |
| `src/UI.Shared/Models/EditMode.cs` | Shared browser UI | Nest API or React UI (split by responsibility): Models/EditMode.cs |
| `src/UI.Shared/Models/UserLoggedInEvent.cs` | Shared browser UI | Nest API or React UI (split by responsibility): Models/UserLoggedInEvent.cs |
| `src/UI.Shared/Models/UserLoggedOutEvent.cs` | Shared browser UI | Nest API or React UI (split by responsibility): Models/UserLoggedOutEvent.cs |
| `src/UI.Shared/Models/WorkOrderManageModel.cs` | Shared browser UI | Nest API or React UI (split by responsibility): Models/WorkOrderManageModel.cs |
| `src/UI.Shared/Models/WorkOrderSearchModel.cs` | Shared browser UI | Nest API or React UI (split by responsibility): Models/WorkOrderSearchModel.cs |
| `src/UI.Shared/NavMenu.Razor.cs` | Shared browser UI | Nest API or React UI (split by responsibility): NavMenu.Razor.cs |
| `src/UI.Shared/NavMenu.razor` | Shared browser UI | Nest API or React UI (split by responsibility): NavMenu.razor |
| `src/UI.Shared/NavMenu.razor.css` | Shared browser UI | Nest API or React UI (split by responsibility): NavMenu.razor.css |
| `src/UI.Shared/NavRailCss.cs` | Shared browser UI | Nest API or React UI (split by responsibility): NavRailCss.cs |
| `src/UI.Shared/Pages/ApplicationChat.razor` | Shared browser UI | Nest API or React UI (split by responsibility): Pages/ApplicationChat.razor |
| `src/UI.Shared/Pages/ApplicationChat.razor.css` | Shared browser UI | Nest API or React UI (split by responsibility): Pages/ApplicationChat.razor.css |
| `src/UI.Shared/Pages/Counter.razor` | Shared browser UI | Nest API or React UI (split by responsibility): Pages/Counter.razor |
| `src/UI.Shared/Pages/Counter.razor.css` | Shared browser UI | Nest API or React UI (split by responsibility): Pages/Counter.razor.css |
| `src/UI.Shared/Pages/FetchData.razor` | Shared browser UI | Nest API or React UI (split by responsibility): Pages/FetchData.razor |
| `src/UI.Shared/Pages/FetchData.razor.cs` | Shared browser UI | Nest API or React UI (split by responsibility): Pages/FetchData.razor.cs |
| `src/UI.Shared/Pages/FetchData.razor.css` | Shared browser UI | Nest API or React UI (split by responsibility): Pages/FetchData.razor.css |
| `src/UI.Shared/Pages/Index.razor` | Shared browser UI | Nest API or React UI (split by responsibility): Pages/Index.razor |
| `src/UI.Shared/Pages/Index.razor.cs` | Shared browser UI | Nest API or React UI (split by responsibility): Pages/Index.razor.cs |
| `src/UI.Shared/Pages/Index.razor.css` | Shared browser UI | Nest API or React UI (split by responsibility): Pages/Index.razor.css |
| `src/UI.Shared/Pages/Login.razor` | Shared browser UI | Nest API or React UI (split by responsibility): Pages/Login.razor |
| `src/UI.Shared/Pages/Login.razor.cs` | Shared browser UI | Nest API or React UI (split by responsibility): Pages/Login.razor.cs |
| `src/UI.Shared/Pages/Login.razor.css` | Shared browser UI | Nest API or React UI (split by responsibility): Pages/Login.razor.css |
| `src/UI.Shared/Pages/SelectListItem.cs` | Shared browser UI | Nest API or React UI (split by responsibility): Pages/SelectListItem.cs |
| `src/UI.Shared/Pages/Settings.razor` | Shared browser UI | Nest API or React UI (split by responsibility): Pages/Settings.razor |
| `src/UI.Shared/Pages/Settings.razor.cs` | Shared browser UI | Nest API or React UI (split by responsibility): Pages/Settings.razor.cs |
| `src/UI.Shared/Pages/Settings.razor.css` | Shared browser UI | Nest API or React UI (split by responsibility): Pages/Settings.razor.css |
| `src/UI.Shared/Pages/WorkOrderManage.razor` | Shared browser UI | Nest API or React UI (split by responsibility): Pages/WorkOrderManage.razor |
| `src/UI.Shared/Pages/WorkOrderManage.razor.cs` | Shared browser UI | Nest API or React UI (split by responsibility): Pages/WorkOrderManage.razor.cs |
| `src/UI.Shared/Pages/WorkOrderManage.razor.css` | Shared browser UI | Nest API or React UI (split by responsibility): Pages/WorkOrderManage.razor.css |
| `src/UI.Shared/Pages/WorkOrderSearch.razor` | Shared browser UI | Nest API or React UI (split by responsibility): Pages/WorkOrderSearch.razor |
| `src/UI.Shared/Pages/WorkOrderSearch.razor.cs` | Shared browser UI | Nest API or React UI (split by responsibility): Pages/WorkOrderSearch.razor.cs |
| `src/UI.Shared/Pages/WorkOrderSearch.razor.css` | Shared browser UI | Nest API or React UI (split by responsibility): Pages/WorkOrderSearch.razor.css |
| `src/UI.Shared/Pages/WorkOrderSpeechHelper.cs` | Shared browser UI | Nest API or React UI (split by responsibility): Pages/WorkOrderSpeechHelper.cs |
| `src/UI.Shared/Services/ThemePreferenceService.cs` | Shared browser UI | Nest API or React UI (split by responsibility): Services/ThemePreferenceService.cs |
| `src/UI.Shared/Services/WorkOrderSearchState.cs` | Shared browser UI | Nest API or React UI (split by responsibility): Services/WorkOrderSearchState.cs |
| `src/UI.Shared/UI.Shared.csproj` | Shared browser UI | Deferred: replace after equivalent behavior lands; retain only as migration reference |
| `src/UI.Shared/_Imports.razor` | Shared browser UI | Nest API or React UI (split by responsibility): _Imports.razor |
| `src/UI.Shared/wwwroot/js/mainLayoutNav.js` | Shared browser UI | Nest API or React UI (split by responsibility): wwwroot/js/mainLayoutNav.js |
| `src/UI.Shared/wwwroot/js/theme.js` | Shared browser UI | Nest API or React UI (split by responsibility): wwwroot/js/theme.js |
| `src/UI/Api/ApiRateLimiting.cs` | UI project | Review and map from UI: Api/ApiRateLimiting.cs |
| `src/UI/Api/ApiRoutes.cs` | UI project | Review and map from UI: Api/ApiRoutes.cs |
| `src/UI/Api/ConditionalGetEtag.cs` | UI project | Review and map from UI: Api/ConditionalGetEtag.cs |
| `src/UI/Api/Controllers/DetailedHealthController.cs` | UI project | Review and map from UI: Api/Controllers/DetailedHealthController.cs |
| `src/UI/Api/Controllers/DiagnosticController.cs` | UI project | Review and map from UI: Api/Controllers/DiagnosticController.cs |
| `src/UI/Api/Controllers/DiagnosticsController.cs` | UI project | Review and map from UI: Api/Controllers/DiagnosticsController.cs |
| `src/UI/Api/Controllers/EchoController.cs` | UI project | Review and map from UI: Api/Controllers/EchoController.cs |
| `src/UI/Api/Controllers/EnvironmentStatusController.cs` | UI project | Review and map from UI: Api/Controllers/EnvironmentStatusController.cs |
| `src/UI/Api/Controllers/FeatureFlagsController.cs` | UI project | Review and map from UI: Api/Controllers/FeatureFlagsController.cs |
| `src/UI/Api/Controllers/MetricsController.cs` | UI project | Review and map from UI: Api/Controllers/MetricsController.cs |
| `src/UI/Api/Controllers/PingController.cs` | UI project | Review and map from UI: Api/Controllers/PingController.cs |
| `src/UI/Api/Controllers/TimeController.cs` | UI project | Review and map from UI: Api/Controllers/TimeController.cs |
| `src/UI/Api/Controllers/TimestampConverterController.cs` | UI project | Review and map from UI: Api/Controllers/TimestampConverterController.cs |
| `src/UI/Api/Controllers/ToolsDueDateCheckController.cs` | UI project | Review and map from UI: Api/Controllers/ToolsDueDateCheckController.cs |
| `src/UI/Api/Controllers/ToolsGuidGeneratorController.cs` | UI project | Review and map from UI: Api/Controllers/ToolsGuidGeneratorController.cs |
| `src/UI/Api/Controllers/ToolsHashController.cs` | UI project | Review and map from UI: Api/Controllers/ToolsHashController.cs |
| `src/UI/Api/Controllers/ToolsRandomController.cs` | UI project | Review and map from UI: Api/Controllers/ToolsRandomController.cs |
| `src/UI/Api/Controllers/ToolsWordCountController.cs` | UI project | Review and map from UI: Api/Controllers/ToolsWordCountController.cs |
| `src/UI/Api/Controllers/ToolsWorkOrderStatusesController.cs` | UI project | Review and map from UI: Api/Controllers/ToolsWorkOrderStatusesController.cs |
| `src/UI/Api/Controllers/VersionController.cs` | UI project | Review and map from UI: Api/Controllers/VersionController.cs |
| `src/UI/Api/Controllers/WeatherForecastController.cs` | UI project | Review and map from UI: Api/Controllers/WeatherForecastController.cs |
| `src/UI/Api/Controllers/WhatDoIHaveController.cs` | UI project | Review and map from UI: Api/Controllers/WhatDoIHaveController.cs |
| `src/UI/Api/Controllers/WorkOrdersBulkImportController.cs` | UI project | Review and map from UI: Api/Controllers/WorkOrdersBulkImportController.cs |
| `src/UI/Api/DetailedHealthModels.cs` | UI project | Review and map from UI: Api/DetailedHealthModels.cs |
| `src/UI/Api/DiagnosticsFeatureFlagsOptions.cs` | UI project | Review and map from UI: Api/DiagnosticsFeatureFlagsOptions.cs |
| `src/UI/Api/DiagnosticsModels.cs` | UI project | Review and map from UI: Api/DiagnosticsModels.cs |
| `src/UI/Api/FeatureFlagsCatalog.cs` | UI project | Review and map from UI: Api/FeatureFlagsCatalog.cs |
| `src/UI/Api/HealthCheck.cs` | UI project | Review and map from UI: Api/HealthCheck.cs |
| `src/UI/Api/HealthReportBuilder.cs` | UI project | Review and map from UI: Api/HealthReportBuilder.cs |
| `src/UI/Api/IDetailedHealthReportProvider.cs` | UI project | Review and map from UI: Api/IDetailedHealthReportProvider.cs |
| `src/UI/Api/IHttpRequestMetricsCounter.cs` | UI project | Review and map from UI: Api/IHttpRequestMetricsCounter.cs |
| `src/UI/Api/MetricsSummaryBuilder.cs` | UI project | Review and map from UI: Api/MetricsSummaryBuilder.cs |
| `src/UI/Api/MetricsSummaryResponse.cs` | UI project | Review and map from UI: Api/MetricsSummaryResponse.cs |
| `src/UI/Api/OutputCachePolicyNames.cs` | UI project | Review and map from UI: Api/OutputCachePolicyNames.cs |
| `src/UI/Api/Pages/Error.cshtml` | UI project | Review and map from UI: Api/Pages/Error.cshtml |
| `src/UI/Api/Pages/Error.cshtml.cs` | UI project | Review and map from UI: Api/Pages/Error.cshtml.cs |
| `src/UI/Api/Program.cs` | UI project | Review and map from UI: Api/Program.cs |
| `src/UI/Api/Properties/launchSettings.json` | UI project | Review and map from UI: Api/Properties/launchSettings.json |
| `src/UI/Api/SimpleHealthResponseBuilder.cs` | UI project | Review and map from UI: Api/SimpleHealthResponseBuilder.cs |
| `src/UI/Api/UI.Api.csproj` | UI project | Deferred: replace after equivalent behavior lands; retain only as migration reference |
| `src/UI/Api/WorkOrderBulkImportModels.cs` | UI project | Review and map from UI: Api/WorkOrderBulkImportModels.cs |
| `src/UI/Api/WorkOrderBulkImportProcessor.cs` | UI project | Review and map from UI: Api/WorkOrderBulkImportProcessor.cs |
| `src/UI/Client/App.razor` | UI project | Review and map from UI: Client/App.razor |
| `src/UI/Client/HealthChecks/HealthCheckTracer.cs` | UI project | Review and map from UI: Client/HealthChecks/HealthCheckTracer.cs |
| `src/UI/Client/HealthChecks/RemotableBusHealthCheck.cs` | UI project | Review and map from UI: Client/HealthChecks/RemotableBusHealthCheck.cs |
| `src/UI/Client/HealthChecks/ServerHealthCheck.cs` | UI project | Review and map from UI: Client/HealthChecks/ServerHealthCheck.cs |
| `src/UI/Client/HealthChecks/ServerHealthCheckQuery.cs` | UI project | Review and map from UI: Client/HealthChecks/ServerHealthCheckQuery.cs |
| `src/UI/Client/IPublisherGateway.cs` | UI project | Review and map from UI: Client/IPublisherGateway.cs |
| `src/UI/Client/Pages/ClientHealthCheck.razor` | UI project | Review and map from UI: Client/Pages/ClientHealthCheck.razor |
| `src/UI/Client/Pages/DetailedClientHealthCheck.razor` | UI project | Review and map from UI: Client/Pages/DetailedClientHealthCheck.razor |
| `src/UI/Client/Pages/NotFound.razor` | UI project | Review and map from UI: Client/Pages/NotFound.razor |
| `src/UI/Client/Program.cs` | UI project | Review and map from UI: Client/Program.cs |
| `src/UI/Client/Properties/launchSettings.json` | UI project | Review and map from UI: Client/Properties/launchSettings.json |
| `src/UI/Client/PublisherGateway.cs` | UI project | Review and map from UI: Client/PublisherGateway.cs |
| `src/UI/Client/RemotableBus.cs` | UI project | Review and map from UI: Client/RemotableBus.cs |
| `src/UI/Client/UI.Client.csproj` | UI project | Deferred: replace after equivalent behavior lands; retain only as migration reference |
| `src/UI/Client/UIClientServiceRegistry.cs` | UI project | Review and map from UI: Client/UIClientServiceRegistry.cs |
| `src/UI/Client/UserSession.cs` | UI project | Review and map from UI: Client/UserSession.cs |
| `src/UI/Client/WasmHostEnvironment.cs` | UI project | Review and map from UI: Client/WasmHostEnvironment.cs |
| `src/UI/Client/_Imports.razor` | UI project | Review and map from UI: Client/_Imports.razor |
| `src/UI/Client/wwwroot/appsettings.json` | UI project | Review and map from UI: Client/wwwroot/appsettings.json |
| `src/UI/Client/wwwroot/css/app.css` | UI project | Review and map from UI: Client/wwwroot/css/app.css |
| `src/UI/Client/wwwroot/css/bootstrap/bootstrap.min.css` | UI project | Review and map from UI: Client/wwwroot/css/bootstrap/bootstrap.min.css |
| `src/UI/Client/wwwroot/css/bootstrap/bootstrap.min.css.map` | UI project | Review and map from UI: Client/wwwroot/css/bootstrap/bootstrap.min.css.map |
| `src/UI/Client/wwwroot/css/open-iconic/FONT-LICENSE` | UI project | Review and map from UI: Client/wwwroot/css/open-iconic/FONT-LICENSE |
| `src/UI/Client/wwwroot/css/open-iconic/ICON-LICENSE` | UI project | Review and map from UI: Client/wwwroot/css/open-iconic/ICON-LICENSE |
| `src/UI/Client/wwwroot/css/open-iconic/README.md` | UI project | Review and map from UI: Client/wwwroot/css/open-iconic/README.md |
| `src/UI/Client/wwwroot/css/open-iconic/font/css/open-iconic-bootstrap.min.css` | UI project | Review and map from UI: Client/wwwroot/css/open-iconic/font/css/open-iconic-bootstrap.min.css |
| `src/UI/Client/wwwroot/css/open-iconic/font/fonts/open-iconic.eot` | UI project | Review and map from UI: Client/wwwroot/css/open-iconic/font/fonts/open-iconic.eot |
| `src/UI/Client/wwwroot/css/open-iconic/font/fonts/open-iconic.otf` | UI project | Review and map from UI: Client/wwwroot/css/open-iconic/font/fonts/open-iconic.otf |
| `src/UI/Client/wwwroot/css/open-iconic/font/fonts/open-iconic.svg` | UI project | Review and map from UI: Client/wwwroot/css/open-iconic/font/fonts/open-iconic.svg |
| `src/UI/Client/wwwroot/css/open-iconic/font/fonts/open-iconic.ttf` | UI project | Review and map from UI: Client/wwwroot/css/open-iconic/font/fonts/open-iconic.ttf |
| `src/UI/Client/wwwroot/css/open-iconic/font/fonts/open-iconic.woff` | UI project | Review and map from UI: Client/wwwroot/css/open-iconic/font/fonts/open-iconic.woff |
| `src/UI/Client/wwwroot/favicon.png` | UI project | Review and map from UI: Client/wwwroot/favicon.png |
| `src/UI/Client/wwwroot/icon-192.png` | UI project | Review and map from UI: Client/wwwroot/icon-192.png |
| `src/UI/Client/wwwroot/index.html` | UI project | Review and map from UI: Client/wwwroot/index.html |
| `src/UI/Client/wwwroot/js/chat.js` | UI project | Review and map from UI: Client/wwwroot/js/chat.js |
| `src/UI/Server/ApiControllerMapping.cs` | UI project | Review and map from UI: Server/ApiControllerMapping.cs |
| `src/UI/Server/ApiKeyAuthenticationMiddleware.cs` | UI project | Review and map from UI: Server/ApiKeyAuthenticationMiddleware.cs |
| `src/UI/Server/ApiKeyAuthenticationOptions.cs` | UI project | Review and map from UI: Server/ApiKeyAuthenticationOptions.cs |
| `src/UI/Server/ApiRateLimitPartitionResolver.cs` | UI project | Review and map from UI: Server/ApiRateLimitPartitionResolver.cs |
| `src/UI/Server/ApiRateLimitingExtensions.cs` | UI project | Review and map from UI: Server/ApiRateLimitingExtensions.cs |
| `src/UI/Server/ApiRateLimitingOptions.cs` | UI project | Review and map from UI: Server/ApiRateLimitingOptions.cs |
| `src/UI/Server/ApiRequestTimeoutOptions.cs` | UI project | Review and map from UI: Server/ApiRequestTimeoutOptions.cs |
| `src/UI/Server/ApiRequestTimeoutsExtensions.cs` | UI project | Review and map from UI: Server/ApiRequestTimeoutsExtensions.cs |
| `src/UI/Server/AutoReformatAgentService.cs` | UI project | Review and map from UI: Server/AutoReformatAgentService.cs |
| `src/UI/Server/CallHealthCheckEndpoint.ps1` | UI project | Review and map from UI: Server/CallHealthCheckEndpoint.ps1 |
| `src/UI/Server/ChatClientConfigQueryHandler.cs` | UI project | Review and map from UI: Server/ChatClientConfigQueryHandler.cs |
| `src/UI/Server/CheckVersion.ps1` | UI project | Review and map from UI: Server/CheckVersion.ps1 |
| `src/UI/Server/Controllers/SingleApiController.cs` | UI project | Review and map from UI: Server/Controllers/SingleApiController.cs |
| `src/UI/Server/CorsExtensions.cs` | UI project | Review and map from UI: Server/CorsExtensions.cs |
| `src/UI/Server/DatabaseConfiguration.cs` | UI project | Review and map from UI: Server/DatabaseConfiguration.cs |
| `src/UI/Server/DetailedHealthCheckResponseWriter.cs` | UI project | Review and map from UI: Server/DetailedHealthCheckResponseWriter.cs |
| `src/UI/Server/DetailedHealthReportProvider.cs` | UI project | Review and map from UI: Server/DetailedHealthReportProvider.cs |
| `src/UI/Server/Generated/Protos/Workorders.cs` | UI project | Deferred: replace after equivalent behavior lands; retain only as migration reference |
| `src/UI/Server/Generated/Protos/WorkordersGrpc.cs` | UI project | Deferred: replace after equivalent behavior lands; retain only as migration reference |
| `src/UI/Server/GlobalUsings.cs` | UI project | Review and map from UI: Server/GlobalUsings.cs |
| `src/UI/Server/Grpc/WorkOrdersGrpcService.cs` | UI project | Review and map from UI: Server/Grpc/WorkOrdersGrpcService.cs |
| `src/UI/Server/Handlers/ServerHealthCheckHandler.cs` | UI project | Review and map from UI: Server/Handlers/ServerHealthCheckHandler.cs |
| `src/UI/Server/HttpRequestMetricsCounter.cs` | UI project | Review and map from UI: Server/HttpRequestMetricsCounter.cs |
| `src/UI/Server/Is64BitProcessHealthCheck.cs` | UI project | Review and map from UI: Server/Is64BitProcessHealthCheck.cs |
| `src/UI/Server/License.xml` | UI project | Review and map from UI: Server/License.xml |
| `src/UI/Server/Middleware/HttpRequestMetricsMiddleware.cs` | UI project | Review and map from UI: Server/Middleware/HttpRequestMetricsMiddleware.cs |
| `src/UI/Server/Middleware/IdempotencyMiddleware.cs` | UI project | Review and map from UI: Server/Middleware/IdempotencyMiddleware.cs |
| `src/UI/Server/Middleware/IdempotencyOptions.cs` | UI project | Review and map from UI: Server/Middleware/IdempotencyOptions.cs |
| `src/UI/Server/Middleware/IdempotencyResponseCache.cs` | UI project | Review and map from UI: Server/Middleware/IdempotencyResponseCache.cs |
| `src/UI/Server/Middleware/WebServiceMessageValidationMiddleware.cs` | UI project | Review and map from UI: Server/Middleware/WebServiceMessageValidationMiddleware.cs |
| `src/UI/Server/NeedsRebootHealthCheck.cs` | UI project | Review and map from UI: Server/NeedsRebootHealthCheck.cs |
| `src/UI/Server/Notifications/RealtimeNotificationHub.cs` | UI project | Review and map from UI: Server/Notifications/RealtimeNotificationHub.cs |
| `src/UI/Server/Notifications/RealtimeNotificationWebSocketMiddleware.cs` | UI project | Review and map from UI: Server/Notifications/RealtimeNotificationWebSocketMiddleware.cs |
| `src/UI/Server/Notifications/ServerRealtimeBus.cs` | UI project | Review and map from UI: Server/Notifications/ServerRealtimeBus.cs |
| `src/UI/Server/ProblemDetailsExceptionHandler.cs` | UI project | Review and map from UI: Server/ProblemDetailsExceptionHandler.cs |
| `src/UI/Server/ProblemDetailsStatusCodePagesExtensions.cs` | UI project | Review and map from UI: Server/ProblemDetailsStatusCodePagesExtensions.cs |
| `src/UI/Server/ProcessThreadCountHealthCheck.cs` | UI project | Review and map from UI: Server/ProcessThreadCountHealthCheck.cs |
| `src/UI/Server/Program.cs` | UI project | Review and map from UI: Server/Program.cs |
| `src/UI/Server/Properties/launchSettings.json` | UI project | Review and map from UI: Server/Properties/launchSettings.json |
| `src/UI/Server/Protos/workorders.proto` | UI project | Review and map from UI: Server/Protos/workorders.proto |
| `src/UI/Server/RateLimiting/RateLimitingMiddleware.cs` | UI project | Review and map from UI: Server/RateLimiting/RateLimitingMiddleware.cs |
| `src/UI/Server/RateLimiting/RateLimitingPipeline.cs` | UI project | Review and map from UI: Server/RateLimiting/RateLimitingPipeline.cs |
| `src/UI/Server/RequestBodyBufferingExtensions.cs` | UI project | Review and map from UI: Server/RequestBodyBufferingExtensions.cs |
| `src/UI/Server/RequestBodyBufferingOptions.cs` | UI project | Review and map from UI: Server/RequestBodyBufferingOptions.cs |
| `src/UI/Server/ScaleInfrastructure.ps1` | UI project | Review and map from UI: Server/ScaleInfrastructure.ps1 |
| `src/UI/Server/ServerApplication.cs` | UI project | Review and map from UI: Server/ServerApplication.cs |
| `src/UI/Server/ServerCorsOptions.cs` | UI project | Review and map from UI: Server/ServerCorsOptions.cs |
| `src/UI/Server/Testing/IdempotencyProbeState.cs` | UI project | Review and map from UI: Server/Testing/IdempotencyProbeState.cs |
| `src/UI/Server/TestingDatabaseStartupFilter.cs` | UI project | Review and map from UI: Server/TestingDatabaseStartupFilter.cs |
| `src/UI/Server/UI.Server.csproj` | UI project | Deferred: replace after equivalent behavior lands; retain only as migration reference |
| `src/UI/Server/UIServiceRegistry.cs` | UI project | Review and map from UI: Server/UIServiceRegistry.cs |
| `src/UI/Server/UiServerWebApplicationMarker.cs` | UI project | Review and map from UI: Server/UiServerWebApplicationMarker.cs |
| `src/UI/Server/Validation/EmployeeGetAllQueryValidator.cs` | UI project | Review and map from UI: Server/Validation/EmployeeGetAllQueryValidator.cs |
| `src/UI/Server/Validation/ServerHealthCheckQueryValidator.cs` | UI project | Review and map from UI: Server/Validation/ServerHealthCheckQueryValidator.cs |
| `src/UI/Server/WebApplicationTestingDatabase.cs` | UI project | Review and map from UI: Server/WebApplicationTestingDatabase.cs |
| `src/UI/Server/WorkOrderReformatAgent.cs` | UI project | Review and map from UI: Server/WorkOrderReformatAgent.cs |
| `src/UI/Server/appsettings.Development.json` | UI project | Review and map from UI: Server/appsettings.Development.json |
| `src/UI/Server/appsettings.Testing.json` | UI project | Review and map from UI: Server/appsettings.Testing.json |
| `src/UI/Server/appsettings.json` | UI project | Review and map from UI: Server/appsettings.json |
| `src/UnitTests/.editorconfig` | Unit and component tests | Jest/Playwright parity suite: .editorconfig |
| `src/UnitTests/Api/ApiRateLimitingWebTests.cs` | Unit and component tests | Jest/Playwright parity suite: Api/ApiRateLimitingWebTests.cs |
| `src/UnitTests/Api/ConditionalGetEtagTests.cs` | Unit and component tests | Jest/Playwright parity suite: Api/ConditionalGetEtagTests.cs |
| `src/UnitTests/Api/CorsEnabledApiWebApplicationFactory.cs` | Unit and component tests | Jest/Playwright parity suite: Api/CorsEnabledApiWebApplicationFactory.cs |
| `src/UnitTests/Api/CorsWebTests.cs` | Unit and component tests | Jest/Playwright parity suite: Api/CorsWebTests.cs |
| `src/UnitTests/Api/IdempotencyMiddlewareWebTests.cs` | Unit and component tests | Jest/Playwright parity suite: Api/IdempotencyMiddlewareWebTests.cs |
| `src/UnitTests/Api/RateLimitedApiWebApplicationFactory.cs` | Unit and component tests | Jest/Playwright parity suite: Api/RateLimitedApiWebApplicationFactory.cs |
| `src/UnitTests/Api/RateLimitingPartitionKeyTests.cs` | Unit and component tests | Jest/Playwright parity suite: Api/RateLimitingPartitionKeyTests.cs |
| `src/UnitTests/Api/TunableApiRateLimitWebApplicationFactory.cs` | Unit and component tests | Jest/Playwright parity suite: Api/TunableApiRateLimitWebApplicationFactory.cs |
| `src/UnitTests/Api/WebServiceMessageValidationMiddlewareWebTests.cs` | Unit and component tests | Jest/Playwright parity suite: Api/WebServiceMessageValidationMiddlewareWebTests.cs |
| `src/UnitTests/Api/WebServiceMessageValidationWebApplicationFactory.cs` | Unit and component tests | Jest/Playwright parity suite: Api/WebServiceMessageValidationWebApplicationFactory.cs |
| `src/UnitTests/BogusOverrides.cs` | Unit and component tests | Jest/Playwright parity suite: BogusOverrides.cs |
| `src/UnitTests/BuildGates/AcceptanceTestsGateDecouplingTests.cs` | Unit and component tests | Jest/Playwright parity suite: BuildGates/AcceptanceTestsGateDecouplingTests.cs |
| `src/UnitTests/BuildGates/CompilerWarningGateTests.cs` | Unit and component tests | Jest/Playwright parity suite: BuildGates/CompilerWarningGateTests.cs |
| `src/UnitTests/BuildGates/CoreCoberturaPresence.cs` | Unit and component tests | Jest/Playwright parity suite: BuildGates/CoreCoberturaPresence.cs |
| `src/UnitTests/BuildGates/CoreCoberturaPresenceTests.cs` | Unit and component tests | Jest/Playwright parity suite: BuildGates/CoreCoberturaPresenceTests.cs |
| `src/UnitTests/BuildGates/CrapGateEvaluator.cs` | Unit and component tests | Jest/Playwright parity suite: BuildGates/CrapGateEvaluator.cs |
| `src/UnitTests/BuildGates/CrapGateEvaluatorTests.cs` | Unit and component tests | Jest/Playwright parity suite: BuildGates/CrapGateEvaluatorTests.cs |
| `src/UnitTests/BuildGates/CrapGateThreshold.cs` | Unit and component tests | Jest/Playwright parity suite: BuildGates/CrapGateThreshold.cs |
| `src/UnitTests/BuildGates/CrapGateThresholdTests.cs` | Unit and component tests | Jest/Playwright parity suite: BuildGates/CrapGateThresholdTests.cs |
| `src/UnitTests/BuildGates/CrapMetricsArtifactWorkflowTests.cs` | Unit and component tests | Jest/Playwright parity suite: BuildGates/CrapMetricsArtifactWorkflowTests.cs |
| `src/UnitTests/BuildGates/CrapProductionScope.cs` | Unit and component tests | Jest/Playwright parity suite: BuildGates/CrapProductionScope.cs |
| `src/UnitTests/BuildGates/CrapProductionScopeTests.cs` | Unit and component tests | Jest/Playwright parity suite: BuildGates/CrapProductionScopeTests.cs |
| `src/UnitTests/BuildGates/DeployWorkflowContainerAppUrlTests.cs` | Unit and component tests | Jest/Playwright parity suite: BuildGates/DeployWorkflowContainerAppUrlTests.cs |
| `src/UnitTests/BuildGates/EnvironmentsDocPresenceTests.cs` | Unit and component tests | Jest/Playwright parity suite: BuildGates/EnvironmentsDocPresenceTests.cs |
| `src/UnitTests/BuildGates/QodanaTestScopeGateTests.cs` | Unit and component tests | Jest/Playwright parity suite: BuildGates/QodanaTestScopeGateTests.cs |
| `src/UnitTests/BuildGates/StabilityKDocPresenceTests.cs` | Unit and component tests | Jest/Playwright parity suite: BuildGates/StabilityKDocPresenceTests.cs |
| `src/UnitTests/BuildGates/WalkthroughGDocPresenceTests.cs` | Unit and component tests | Jest/Playwright parity suite: BuildGates/WalkthroughGDocPresenceTests.cs |
| `src/UnitTests/Core/Handlers/WorkOrderCountByStatusQueryHandlerTests.cs` | Unit and component tests | Jest/Playwright parity suite: Core/Handlers/WorkOrderCountByStatusQueryHandlerTests.cs |
| `src/UnitTests/Core/Import/WorkOrderBulkImportCsvParserTests.cs` | Unit and component tests | Jest/Playwright parity suite: Core/Import/WorkOrderBulkImportCsvParserTests.cs |
| `src/UnitTests/Core/Model/EmployeeTests.cs` | Unit and component tests | Jest/Playwright parity suite: Core/Model/EmployeeTests.cs |
| `src/UnitTests/Core/Model/EntityBaseTests.cs` | Unit and component tests | Jest/Playwright parity suite: Core/Model/EntityBaseTests.cs |
| `src/UnitTests/Core/Model/RoleTests.cs` | Unit and component tests | Jest/Playwright parity suite: Core/Model/RoleTests.cs |
| `src/UnitTests/Core/Model/StateCommands/AddAttachmentMetadataCommandTests.cs` | Unit and component tests | Jest/Playwright parity suite: Core/Model/StateCommands/AddAttachmentMetadataCommandTests.cs |
| `src/UnitTests/Core/Model/StateCommands/AssignedToCancelledCommandTests.cs` | Unit and component tests | Jest/Playwright parity suite: Core/Model/StateCommands/AssignedToCancelledCommandTests.cs |
| `src/UnitTests/Core/Model/StateCommands/AssignedToInProgressCommandTests.cs` | Unit and component tests | Jest/Playwright parity suite: Core/Model/StateCommands/AssignedToInProgressCommandTests.cs |
| `src/UnitTests/Core/Model/StateCommands/DraftToAssignedCommandTests.cs` | Unit and component tests | Jest/Playwright parity suite: Core/Model/StateCommands/DraftToAssignedCommandTests.cs |
| `src/UnitTests/Core/Model/StateCommands/InProgressToAssignCommandTests.cs` | Unit and component tests | Jest/Playwright parity suite: Core/Model/StateCommands/InProgressToAssignCommandTests.cs |
| `src/UnitTests/Core/Model/StateCommands/InProgressToCompleteCommandTests.cs` | Unit and component tests | Jest/Playwright parity suite: Core/Model/StateCommands/InProgressToCompleteCommandTests.cs |
| `src/UnitTests/Core/Model/StateCommands/SaveDraftCommandTests.cs` | Unit and component tests | Jest/Playwright parity suite: Core/Model/StateCommands/SaveDraftCommandTests.cs |
| `src/UnitTests/Core/Model/StateCommands/StateCommandBaseTests.cs` | Unit and component tests | Jest/Playwright parity suite: Core/Model/StateCommands/StateCommandBaseTests.cs |
| `src/UnitTests/Core/Model/WorkOrderAttachmentTests.cs` | Unit and component tests | Jest/Playwright parity suite: Core/Model/WorkOrderAttachmentTests.cs |
| `src/UnitTests/Core/Model/WorkOrderStatusTests.cs` | Unit and component tests | Jest/Playwright parity suite: Core/Model/WorkOrderStatusTests.cs |
| `src/UnitTests/Core/Model/WorkOrderTests.cs` | Unit and component tests | Jest/Playwright parity suite: Core/Model/WorkOrderTests.cs |
| `src/UnitTests/Core/Queries/RemotableRequestTests.cs` | Unit and component tests | Jest/Playwright parity suite: Core/Queries/RemotableRequestTests.cs |
| `src/UnitTests/Core/Services/DueDateUrgencyCalculatorTests.cs` | Unit and component tests | Jest/Playwright parity suite: Core/Services/DueDateUrgencyCalculatorTests.cs |
| `src/UnitTests/Core/Services/StateCommandListTests.cs` | Unit and component tests | Jest/Playwright parity suite: Core/Services/StateCommandListTests.cs |
| `src/UnitTests/Core/Services/WorkOrderBuilderTests.cs` | Unit and component tests | Jest/Playwright parity suite: Core/Services/WorkOrderBuilderTests.cs |
| `src/UnitTests/Core/Services/WorkOrderNumberGeneratorTests.cs` | Unit and component tests | Jest/Playwright parity suite: Core/Services/WorkOrderNumberGeneratorTests.cs |
| `src/UnitTests/Core/Validation/CreateDatedWorkOrdersCommandValidatorTests.cs` | Unit and component tests | Jest/Playwright parity suite: Core/Validation/CreateDatedWorkOrdersCommandValidatorTests.cs |
| `src/UnitTests/Core/Validation/EmployeeByUserNameQueryValidatorTests.cs` | Unit and component tests | Jest/Playwright parity suite: Core/Validation/EmployeeByUserNameQueryValidatorTests.cs |
| `src/UnitTests/Core/Validation/HealthCheckRemotableRequestValidatorTests.cs` | Unit and component tests | Jest/Playwright parity suite: Core/Validation/HealthCheckRemotableRequestValidatorTests.cs |
| `src/UnitTests/Core/Validation/RemotableRequestValidatorCoverageTests.cs` | Unit and component tests | Jest/Playwright parity suite: Core/Validation/RemotableRequestValidatorCoverageTests.cs |
| `src/UnitTests/Core/Validation/WorkOrderAttachmentsQueryValidatorTests.cs` | Unit and component tests | Jest/Playwright parity suite: Core/Validation/WorkOrderAttachmentsQueryValidatorTests.cs |
| `src/UnitTests/Core/Validation/WorkOrderCountByStatusQueryValidatorTests.cs` | Unit and component tests | Jest/Playwright parity suite: Core/Validation/WorkOrderCountByStatusQueryValidatorTests.cs |
| `src/UnitTests/Database/DatabaseConsoleTests.cs` | Unit and component tests | Jest/Playwright parity suite: Database/DatabaseConsoleTests.cs |
| `src/UnitTests/Database/DatabaseUpgradeLoggingTests.cs` | Unit and component tests | Jest/Playwright parity suite: Database/DatabaseUpgradeLoggingTests.cs |
| `src/UnitTests/LlmGateway/ApplicationChatHandlerTests.cs` | Unit and component tests | Jest/Playwright parity suite: LlmGateway/ApplicationChatHandlerTests.cs |
| `src/UnitTests/LlmGateway/CanConnectToLlmServerHealthCheckTests.cs` | Unit and component tests | Jest/Playwright parity suite: LlmGateway/CanConnectToLlmServerHealthCheckTests.cs |
| `src/UnitTests/LlmGateway/ChatActivityTracingTests.cs` | Unit and component tests | Jest/Playwright parity suite: LlmGateway/ChatActivityTracingTests.cs |
| `src/UnitTests/LlmGateway/LlmGatewayHelperTests.cs` | Unit and component tests | Jest/Playwright parity suite: LlmGateway/LlmGatewayHelperTests.cs |
| `src/UnitTests/LlmGateway/TracingChatClientTests.cs` | Unit and component tests | Jest/Playwright parity suite: LlmGateway/TracingChatClientTests.cs |
| `src/UnitTests/LlmGateway/TranslationServiceTests.cs` | Unit and component tests | Jest/Playwright parity suite: LlmGateway/TranslationServiceTests.cs |
| `src/UnitTests/LlmGateway/WorkOrderChatHandlerTests.cs` | Unit and component tests | Jest/Playwright parity suite: LlmGateway/WorkOrderChatHandlerTests.cs |
| `src/UnitTests/LlmGateway/WorkOrderToolTests.cs` | Unit and component tests | Jest/Playwright parity suite: LlmGateway/WorkOrderToolTests.cs |
| `src/UnitTests/Logging/SerilogJsonFormattingTests.cs` | Unit and component tests | Jest/Playwright parity suite: Logging/SerilogJsonFormattingTests.cs |
| `src/UnitTests/McpServer/NullDistributedBusTests.cs` | Unit and component tests | Jest/Playwright parity suite: McpServer/NullDistributedBusTests.cs |
| `src/UnitTests/ObjectMother.cs` | Unit and component tests | Jest/Playwright parity suite: ObjectMother.cs |
| `src/UnitTests/ServiceDefaults/LocalTelemetryLoggerProviderTests.cs` | Unit and component tests | Jest/Playwright parity suite: ServiceDefaults/LocalTelemetryLoggerProviderTests.cs |
| `src/UnitTests/ServiceDefaults/LogEntryErrorTests.cs` | Unit and component tests | Jest/Playwright parity suite: ServiceDefaults/LogEntryErrorTests.cs |
| `src/UnitTests/ServiceDefaults/NamespaceAlignmentTests.cs` | Unit and component tests | Jest/Playwright parity suite: ServiceDefaults/NamespaceAlignmentTests.cs |
| `src/UnitTests/ServiceDefaults/SerilogRegistrationTests.cs` | Unit and component tests | Jest/Playwright parity suite: ServiceDefaults/SerilogRegistrationTests.cs |
| `src/UnitTests/ServiceDefaults/ServiceDefaultsTelemetryTests.cs` | Unit and component tests | Jest/Playwright parity suite: ServiceDefaults/ServiceDefaultsTelemetryTests.cs |
| `src/UnitTests/UI.Api/DetailedHealthControllerTests.cs` | Unit and component tests | Jest/Playwright parity suite: UI.Api/DetailedHealthControllerTests.cs |
| `src/UnitTests/UI.Api/DiagnosticsControllerTests.cs` | Unit and component tests | Jest/Playwright parity suite: UI.Api/DiagnosticsControllerTests.cs |
| `src/UnitTests/UI.Api/EchoControllerTests.cs` | Unit and component tests | Jest/Playwright parity suite: UI.Api/EchoControllerTests.cs |
| `src/UnitTests/UI.Api/EnvironmentStatusControllerTests.cs` | Unit and component tests | Jest/Playwright parity suite: UI.Api/EnvironmentStatusControllerTests.cs |
| `src/UnitTests/UI.Api/ErrorModelTests.cs` | Unit and component tests | Jest/Playwright parity suite: UI.Api/ErrorModelTests.cs |
| `src/UnitTests/UI.Api/ErrorPageStaticAssetsTests.cs` | Unit and component tests | Jest/Playwright parity suite: UI.Api/ErrorPageStaticAssetsTests.cs |
| `src/UnitTests/UI.Api/FeatureFlagsControllerTests.cs` | Unit and component tests | Jest/Playwright parity suite: UI.Api/FeatureFlagsControllerTests.cs |
| `src/UnitTests/UI.Api/HttpRequestMetricsCounterTests.cs` | Unit and component tests | Jest/Playwright parity suite: UI.Api/HttpRequestMetricsCounterTests.cs |
| `src/UnitTests/UI.Api/MetricsControllerTests.cs` | Unit and component tests | Jest/Playwright parity suite: UI.Api/MetricsControllerTests.cs |
| `src/UnitTests/UI.Api/MetricsSummaryBuilderTests.cs` | Unit and component tests | Jest/Playwright parity suite: UI.Api/MetricsSummaryBuilderTests.cs |
| `src/UnitTests/UI.Api/PingControllerTests.cs` | Unit and component tests | Jest/Playwright parity suite: UI.Api/PingControllerTests.cs |
| `src/UnitTests/UI.Api/TimeControllerTests.cs` | Unit and component tests | Jest/Playwright parity suite: UI.Api/TimeControllerTests.cs |
| `src/UnitTests/UI.Api/TimestampConverterControllerTests.cs` | Unit and component tests | Jest/Playwright parity suite: UI.Api/TimestampConverterControllerTests.cs |
| `src/UnitTests/UI.Api/ToolsDueDateCheckControllerTests.cs` | Unit and component tests | Jest/Playwright parity suite: UI.Api/ToolsDueDateCheckControllerTests.cs |
| `src/UnitTests/UI.Api/ToolsGuidGeneratorControllerTests.cs` | Unit and component tests | Jest/Playwright parity suite: UI.Api/ToolsGuidGeneratorControllerTests.cs |
| `src/UnitTests/UI.Api/ToolsHashControllerTests.cs` | Unit and component tests | Jest/Playwright parity suite: UI.Api/ToolsHashControllerTests.cs |
| `src/UnitTests/UI.Api/ToolsRandomControllerTests.cs` | Unit and component tests | Jest/Playwright parity suite: UI.Api/ToolsRandomControllerTests.cs |
| `src/UnitTests/UI.Api/ToolsWordCountControllerTests.cs` | Unit and component tests | Jest/Playwright parity suite: UI.Api/ToolsWordCountControllerTests.cs |
| `src/UnitTests/UI.Api/ToolsWorkOrderStatusesControllerTests.cs` | Unit and component tests | Jest/Playwright parity suite: UI.Api/ToolsWorkOrderStatusesControllerTests.cs |
| `src/UnitTests/UI.Api/VersionControllerTests.cs` | Unit and component tests | Jest/Playwright parity suite: UI.Api/VersionControllerTests.cs |
| `src/UnitTests/UI.Api/WeatherForecastControllerTests.cs` | Unit and component tests | Jest/Playwright parity suite: UI.Api/WeatherForecastControllerTests.cs |
| `src/UnitTests/UI.Api/WhatDoIHaveControllerTests.cs` | Unit and component tests | Jest/Playwright parity suite: UI.Api/WhatDoIHaveControllerTests.cs |
| `src/UnitTests/UI.Api/WorkOrdersBulkImportControllerTests.cs` | Unit and component tests | Jest/Playwright parity suite: UI.Api/WorkOrdersBulkImportControllerTests.cs |
| `src/UnitTests/UI.Client/Authentication/CustomAuthenticationStateProviderTests.cs` | Unit and component tests | Jest/Playwright parity suite: UI.Client/Authentication/CustomAuthenticationStateProviderTests.cs |
| `src/UnitTests/UI.Client/Authentication/LocalStorageUserSessionStoreTests.cs` | Unit and component tests | Jest/Playwright parity suite: UI.Client/Authentication/LocalStorageUserSessionStoreTests.cs |
| `src/UnitTests/UI.Client/Authentication/StubUserSessionStore.cs` | Unit and component tests | Jest/Playwright parity suite: UI.Client/Authentication/StubUserSessionStore.cs |
| `src/UnitTests/UI.Client/ClientHealthCheckPageTests.cs` | Unit and component tests | Jest/Playwright parity suite: UI.Client/ClientHealthCheckPageTests.cs |
| `src/UnitTests/UI.Client/ClientHealthCheckTests.cs` | Unit and component tests | Jest/Playwright parity suite: UI.Client/ClientHealthCheckTests.cs |
| `src/UnitTests/UI.Client/IndexHtmlThemeScriptTests.cs` | Unit and component tests | Jest/Playwright parity suite: UI.Client/IndexHtmlThemeScriptTests.cs |
| `src/UnitTests/UI.Client/PublisherGatewayTests.cs` | Unit and component tests | Jest/Playwright parity suite: UI.Client/PublisherGatewayTests.cs |
| `src/UnitTests/UI.Client/RemotableBusTests.cs` | Unit and component tests | Jest/Playwright parity suite: UI.Client/RemotableBusTests.cs |
| `src/UnitTests/UI.Client/UserSessionTests.cs` | Unit and component tests | Jest/Playwright parity suite: UI.Client/UserSessionTests.cs |
| `src/UnitTests/UI.Server/ApiKeyAuthenticationMiddlewarePublicPathTests.cs` | Unit and component tests | Jest/Playwright parity suite: UI.Server/ApiKeyAuthenticationMiddlewarePublicPathTests.cs |
| `src/UnitTests/UI.Server/ApiKeyAuthenticationMiddlewareTests.cs` | Unit and component tests | Jest/Playwright parity suite: UI.Server/ApiKeyAuthenticationMiddlewareTests.cs |
| `src/UnitTests/UI.Server/ApiKeyAuthenticationWebTests.cs` | Unit and component tests | Jest/Playwright parity suite: UI.Server/ApiKeyAuthenticationWebTests.cs |
| `src/UnitTests/UI.Server/ApiKeyProtectedWebApplicationFactory.cs` | Unit and component tests | Jest/Playwright parity suite: UI.Server/ApiKeyProtectedWebApplicationFactory.cs |
| `src/UnitTests/UI.Server/ApiResponseCompressionWebTests.cs` | Unit and component tests | Jest/Playwright parity suite: UI.Server/ApiResponseCompressionWebTests.cs |
| `src/UnitTests/UI.Server/ApiVersioningEndpointTests.cs` | Unit and component tests | Jest/Playwright parity suite: UI.Server/ApiVersioningEndpointTests.cs |
| `src/UnitTests/UI.Server/ApiVersioningRoutingWebApplicationFactory.cs` | Unit and component tests | Jest/Playwright parity suite: UI.Server/ApiVersioningRoutingWebApplicationFactory.cs |
| `src/UnitTests/UI.Server/AutoReformatAgentServiceTests.cs` | Unit and component tests | Jest/Playwright parity suite: UI.Server/AutoReformatAgentServiceTests.cs |
| `src/UnitTests/UI.Server/BusTests.cs` | Unit and component tests | Jest/Playwright parity suite: UI.Server/BusTests.cs |
| `src/UnitTests/UI.Server/CorrelationIdMiddlewareWebTests.cs` | Unit and component tests | Jest/Playwright parity suite: UI.Server/CorrelationIdMiddlewareWebTests.cs |
| `src/UnitTests/UI.Server/DatabaseConfigurationTests.cs` | Unit and component tests | Jest/Playwright parity suite: UI.Server/DatabaseConfigurationTests.cs |
| `src/UnitTests/UI.Server/EtagConditionalGetWebTests.cs` | Unit and component tests | Jest/Playwright parity suite: UI.Server/EtagConditionalGetWebTests.cs |
| `src/UnitTests/UI.Server/IdempotencyMiddlewareTests.cs` | Unit and component tests | Jest/Playwright parity suite: UI.Server/IdempotencyMiddlewareTests.cs |
| `src/UnitTests/UI.Server/OutputCacheEndpointTests.cs` | Unit and component tests | Jest/Playwright parity suite: UI.Server/OutputCacheEndpointTests.cs |
| `src/UnitTests/UI.Server/ProblemDetailsExceptionHandlerTests.cs` | Unit and component tests | Jest/Playwright parity suite: UI.Server/ProblemDetailsExceptionHandlerTests.cs |
| `src/UnitTests/UI.Server/ProblemDetailsStatusCodePagesExtensionsTests.cs` | Unit and component tests | Jest/Playwright parity suite: UI.Server/ProblemDetailsStatusCodePagesExtensionsTests.cs |
| `src/UnitTests/UI.Server/ProblemDetailsWebTests.cs` | Unit and component tests | Jest/Playwright parity suite: UI.Server/ProblemDetailsWebTests.cs |
| `src/UnitTests/UI.Server/RateLimitingMiddlewareTests.cs` | Unit and component tests | Jest/Playwright parity suite: UI.Server/RateLimitingMiddlewareTests.cs |
| `src/UnitTests/UI.Server/RealtimeNotificationHubTests.cs` | Unit and component tests | Jest/Playwright parity suite: UI.Server/RealtimeNotificationHubTests.cs |
| `src/UnitTests/UI.Server/RealtimeNotificationWebSocketMiddlewareTests.cs` | Unit and component tests | Jest/Playwright parity suite: UI.Server/RealtimeNotificationWebSocketMiddlewareTests.cs |
| `src/UnitTests/UI.Server/RequestBodyBufferingDisabledWebApplicationFactory.cs` | Unit and component tests | Jest/Playwright parity suite: UI.Server/RequestBodyBufferingDisabledWebApplicationFactory.cs |
| `src/UnitTests/UI.Server/RequestBodyBufferingExtensionsTests.cs` | Unit and component tests | Jest/Playwright parity suite: UI.Server/RequestBodyBufferingExtensionsTests.cs |
| `src/UnitTests/UI.Server/RequestBodyBufferingWebTests.cs` | Unit and component tests | Jest/Playwright parity suite: UI.Server/RequestBodyBufferingWebTests.cs |
| `src/UnitTests/UI.Server/RequestDecompressionWebTests.cs` | Unit and component tests | Jest/Playwright parity suite: UI.Server/RequestDecompressionWebTests.cs |
| `src/UnitTests/UI.Server/RequestTimeoutApiWebTests.cs` | Unit and component tests | Jest/Playwright parity suite: UI.Server/RequestTimeoutApiWebTests.cs |
| `src/UnitTests/UI.Server/ServerHealthCheckHandlerTests.cs` | Unit and component tests | Jest/Playwright parity suite: UI.Server/ServerHealthCheckHandlerTests.cs |
| `src/UnitTests/UI.Server/ServerRealtimeBusTests.cs` | Unit and component tests | Jest/Playwright parity suite: UI.Server/ServerRealtimeBusTests.cs |
| `src/UnitTests/UI.Server/SingleApiControllerTests.cs` | Unit and component tests | Jest/Playwright parity suite: UI.Server/SingleApiControllerTests.cs |
| `src/UnitTests/UI.Server/Validation/ServerHealthCheckQueryValidatorTests.cs` | Unit and component tests | Jest/Playwright parity suite: UI.Server/Validation/ServerHealthCheckQueryValidatorTests.cs |
| `src/UnitTests/UI.Server/WebServiceMessageValidationMiddlewareTests.cs` | Unit and component tests | Jest/Playwright parity suite: UI.Server/WebServiceMessageValidationMiddlewareTests.cs |
| `src/UnitTests/UI.Server/WorkOrderReformatAgentTests.cs` | Unit and component tests | Jest/Playwright parity suite: UI.Server/WorkOrderReformatAgentTests.cs |
| `src/UnitTests/UI.Server/WorkOrdersGrpcServiceMappingTests.cs` | Unit and component tests | Jest/Playwright parity suite: UI.Server/WorkOrdersGrpcServiceMappingTests.cs |
| `src/UnitTests/UI.Shared/Components/LoginLinkBlinkStyleTests.cs` | Unit and component tests | Jest/Playwright parity suite: UI.Shared/Components/LoginLinkBlinkStyleTests.cs |
| `src/UnitTests/UI.Shared/Components/LogoutTests.cs` | Unit and component tests | Jest/Playwright parity suite: UI.Shared/Components/LogoutTests.cs |
| `src/UnitTests/UI.Shared/Components/MyWorkOrdersTests.cs` | Unit and component tests | Jest/Playwright parity suite: UI.Shared/Components/MyWorkOrdersTests.cs |
| `src/UnitTests/UI.Shared/LoginDisplayNameFormatterTests.cs` | Unit and component tests | Jest/Playwright parity suite: UI.Shared/LoginDisplayNameFormatterTests.cs |
| `src/UnitTests/UI.Shared/MainLayoutTests.cs` | Unit and component tests | Jest/Playwright parity suite: UI.Shared/MainLayoutTests.cs |
| `src/UnitTests/UI.Shared/Models/WorkOrderManageModelInstructionsTests.cs` | Unit and component tests | Jest/Playwright parity suite: UI.Shared/Models/WorkOrderManageModelInstructionsTests.cs |
| `src/UnitTests/UI.Shared/Models/WorkOrderManageModelRoomNumberTests.cs` | Unit and component tests | Jest/Playwright parity suite: UI.Shared/Models/WorkOrderManageModelRoomNumberTests.cs |
| `src/UnitTests/UI.Shared/NavRailCssTests.cs` | Unit and component tests | Jest/Playwright parity suite: UI.Shared/NavRailCssTests.cs |
| `src/UnitTests/UI.Shared/Pages/ApplicationChatTests.cs` | Unit and component tests | Jest/Playwright parity suite: UI.Shared/Pages/ApplicationChatTests.cs |
| `src/UnitTests/UI.Shared/Pages/FetchDataTests.cs` | Unit and component tests | Jest/Playwright parity suite: UI.Shared/Pages/FetchDataTests.cs |
| `src/UnitTests/UI.Shared/Pages/IndexPageTests.cs` | Unit and component tests | Jest/Playwright parity suite: UI.Shared/Pages/IndexPageTests.cs |
| `src/UnitTests/UI.Shared/Pages/LoginPageTests.cs` | Unit and component tests | Jest/Playwright parity suite: UI.Shared/Pages/LoginPageTests.cs |
| `src/UnitTests/UI.Shared/Pages/SettingsTests.cs` | Unit and component tests | Jest/Playwright parity suite: UI.Shared/Pages/SettingsTests.cs |
| `src/UnitTests/UI.Shared/Pages/StubBus.cs` | Unit and component tests | Jest/Playwright parity suite: UI.Shared/Pages/StubBus.cs |
| `src/UnitTests/UI.Shared/Pages/StubUiBus.cs` | Unit and component tests | Jest/Playwright parity suite: UI.Shared/Pages/StubUiBus.cs |
| `src/UnitTests/UI.Shared/Pages/WorkOrderDueDateStyleTests.cs` | Unit and component tests | Jest/Playwright parity suite: UI.Shared/Pages/WorkOrderDueDateStyleTests.cs |
| `src/UnitTests/UI.Shared/Pages/WorkOrderManageAttachmentsTests.cs` | Unit and component tests | Jest/Playwright parity suite: UI.Shared/Pages/WorkOrderManageAttachmentsTests.cs |
| `src/UnitTests/UI.Shared/Pages/WorkOrderManageDescriptionCharCountTests.cs` | Unit and component tests | Jest/Playwright parity suite: UI.Shared/Pages/WorkOrderManageDescriptionCharCountTests.cs |
| `src/UnitTests/UI.Shared/Pages/WorkOrderManageDictationTests.cs` | Unit and component tests | Jest/Playwright parity suite: UI.Shared/Pages/WorkOrderManageDictationTests.cs |
| `src/UnitTests/UI.Shared/Pages/WorkOrderManageEventBusNotifyTests.cs` | Unit and component tests | Jest/Playwright parity suite: UI.Shared/Pages/WorkOrderManageEventBusNotifyTests.cs |
| `src/UnitTests/UI.Shared/Pages/WorkOrderManageInstructionsFieldTests.cs` | Unit and component tests | Jest/Playwright parity suite: UI.Shared/Pages/WorkOrderManageInstructionsFieldTests.cs |
| `src/UnitTests/UI.Shared/Pages/WorkOrderManageRoomFieldTests.cs` | Unit and component tests | Jest/Playwright parity suite: UI.Shared/Pages/WorkOrderManageRoomFieldTests.cs |
| `src/UnitTests/UI.Shared/Pages/WorkOrderManageSpeechTests.cs` | Unit and component tests | Jest/Playwright parity suite: UI.Shared/Pages/WorkOrderManageSpeechTests.cs |
| `src/UnitTests/UI.Shared/Pages/WorkOrderManageSubmitTests.cs` | Unit and component tests | Jest/Playwright parity suite: UI.Shared/Pages/WorkOrderManageSubmitTests.cs |
| `src/UnitTests/UI.Shared/Pages/WorkOrderSearchTests.cs` | Unit and component tests | Jest/Playwright parity suite: UI.Shared/Pages/WorkOrderSearchTests.cs |
| `src/UnitTests/UI.Shared/Pages/WorkOrderSpeechHelperTests.cs` | Unit and component tests | Jest/Playwright parity suite: UI.Shared/Pages/WorkOrderSpeechHelperTests.cs |
| `src/UnitTests/UI.Shared/Services/ThemePreferenceServiceTests.cs` | Unit and component tests | Jest/Playwright parity suite: UI.Shared/Services/ThemePreferenceServiceTests.cs |
| `src/UnitTests/UI.Shared/UiSharedHelperTests.cs` | Unit and component tests | Jest/Playwright parity suite: UI.Shared/UiSharedHelperTests.cs |
| `src/UnitTests/UI/Api/DetailedHealthEtagFingerprintTests.cs` | Unit and component tests | Jest/Playwright parity suite: UI/Api/DetailedHealthEtagFingerprintTests.cs |
| `src/UnitTests/UI/Api/HealthReportBuilderTests.cs` | Unit and component tests | Jest/Playwright parity suite: UI/Api/HealthReportBuilderTests.cs |
| `src/UnitTests/UI/Api/SimpleHealthResponseBuilderTests.cs` | Unit and component tests | Jest/Playwright parity suite: UI/Api/SimpleHealthResponseBuilderTests.cs |
| `src/UnitTests/UI/Server/ChatClientConfigQueryHandlerTests.cs` | Unit and component tests | Jest/Playwright parity suite: UI/Server/ChatClientConfigQueryHandlerTests.cs |
| `src/UnitTests/UI/Server/DetailedHealthCheckResponseWriterTests.cs` | Unit and component tests | Jest/Playwright parity suite: UI/Server/DetailedHealthCheckResponseWriterTests.cs |
| `src/UnitTests/UI/Server/DetailedHealthReportProviderTests.cs` | Unit and component tests | Jest/Playwright parity suite: UI/Server/DetailedHealthReportProviderTests.cs |
| `src/UnitTests/UI/Server/NeedsRebootHealthCheckTests.cs` | Unit and component tests | Jest/Playwright parity suite: UI/Server/NeedsRebootHealthCheckTests.cs |
| `src/UnitTests/UI/Server/ProcessThreadCountHealthCheckTests.cs` | Unit and component tests | Jest/Playwright parity suite: UI/Server/ProcessThreadCountHealthCheckTests.cs |
| `src/UnitTests/UnitTests.csproj` | Unit and component tests | Deferred: replace after equivalent behavior lands; retain only as migration reference |
| `src/UnitTests/Usings.cs` | Unit and component tests | Jest/Playwright parity suite: Usings.cs |
| `src/UnitTests/Worker/AiBotWorkOrderSagaTests.cs` | Unit and component tests | Jest/Playwright parity suite: Worker/AiBotWorkOrderSagaTests.cs |
| `src/UnitTests/Worker/StubMessageHandlerContext.cs` | Unit and component tests | Jest/Playwright parity suite: Worker/StubMessageHandlerContext.cs |
| `src/UnitTests/Worker/StubMessageHandlerContextTests.cs` | Unit and component tests | Jest/Playwright parity suite: Worker/StubMessageHandlerContextTests.cs |
| `src/UnitTests/Worker/WorkOrderEndpointTests.cs` | Unit and component tests | Jest/Playwright parity suite: Worker/WorkOrderEndpointTests.cs |
| `src/UnitTests/Worker/WorkerHandlerTests.cs` | Unit and component tests | Jest/Playwright parity suite: Worker/WorkerHandlerTests.cs |
| `src/UnitTests/Worker/WorkerRemotableBusTests.cs` | Unit and component tests | Jest/Playwright parity suite: Worker/WorkerRemotableBusTests.cs |
| `src/Worker/Handlers/AiBotHandler.cs` | Background processing | Nest worker/outbox: Handlers/AiBotHandler.cs |
| `src/Worker/Handlers/EventHandler.cs` | Background processing | Nest worker/outbox: Handlers/EventHandler.cs |
| `src/Worker/Handlers/TracerBulletHandler.cs` | Background processing | Nest worker/outbox: Handlers/TracerBulletHandler.cs |
| `src/Worker/Messaging/RemotableBus.cs` | Background processing | Nest worker/outbox: Messaging/RemotableBus.cs |
| `src/Worker/Program.cs` | Background processing | Nest worker/outbox: Program.cs |
| `src/Worker/Properties/launchSettings.json` | Background processing | Nest worker/outbox: Properties/launchSettings.json |
| `src/Worker/Sagas/AiBotWorkerOrder/AiBotWorkOrderSaga.cs` | Background processing | Nest worker/outbox: Sagas/AiBotWorkerOrder/AiBotWorkOrderSaga.cs |
| `src/Worker/Sagas/AiBotWorkerOrder/AiBotWorkOrderSagaState.cs` | Background processing | Nest worker/outbox: Sagas/AiBotWorkerOrder/AiBotWorkOrderSagaState.cs |
| `src/Worker/Sagas/AiBotWorkerOrder/Commands/StartAiBotWorkOrderSagaCommand.cs` | Background processing | Nest worker/outbox: Sagas/AiBotWorkerOrder/Commands/StartAiBotWorkOrderSagaCommand.cs |
| `src/Worker/Sagas/AiBotWorkerOrder/Events/AiBotCompletedWorkOrderEvent.cs` | Background processing | Nest worker/outbox: Sagas/AiBotWorkerOrder/Events/AiBotCompletedWorkOrderEvent.cs |
| `src/Worker/Sagas/AiBotWorkerOrder/Events/AiBotStartedWorkOrderEvent.cs` | Background processing | Nest worker/outbox: Sagas/AiBotWorkerOrder/Events/AiBotStartedWorkOrderEvent.cs |
| `src/Worker/Sagas/AiBotWorkerOrder/Events/AiBotUpdatedWorkerOrderEvent.cs` | Background processing | Nest worker/outbox: Sagas/AiBotWorkerOrder/Events/AiBotUpdatedWorkerOrderEvent.cs |
| `src/Worker/WorkOrderEndpoint.cs` | Background processing | Nest worker/outbox: WorkOrderEndpoint.cs |
| `src/Worker/Worker.csproj` | Background processing | Deferred: replace after equivalent behavior lands; retain only as migration reference |
| `src/Worker/appsettings.Development.json` | Background processing | Nest worker/outbox: appsettings.Development.json |
| `src/Worker/appsettings.json` | Background processing | Nest worker/outbox: appsettings.json |
| `src/pure-azdo-pipeline.yml` | pure-azdo-pipeline.yml project | Review and map from pure-azdo-pipeline.yml:  |
| `src/quickstart.md` | quickstart.md project | Review and map from quickstart.md:  |
| `src/src/IntegrationTests/LlmGateway/CanConnectToLlmServerHealthCheckTests.cs` | src project | Review and map from src: IntegrationTests/LlmGateway/CanConnectToLlmServerHealthCheckTests.cs |
