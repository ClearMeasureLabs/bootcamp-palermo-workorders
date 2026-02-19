# AssignedToInProgressCommand Sequence Diagram

```mermaid
sequenceDiagram
autonumber

actor user as "User"
participant api as "SingleApiController (UI.Server)"
participant sBus as "Bus : IBus (UI.Server)"
participant mediator as "IMediator"
participant empHandler as "EmployeeQueryHandler"
participant woHandler as "WorkOrderQueryHandler"
participant cmd as "AssignedToInProgressCommand"
participant cmdHandler as "StateCommandHandler"
participant cmdBase as "StateCommandBase"
participant cmdContext as "StateCommandContext"
participant order as "WorkOrder"
participant result as "StateCommandResult"
participant db as "DataContext / SQL Server"

user->>api: User input
api->>sBus: Send(AssignedToInProgressCommand)
sBus->>mediator: Send(AssignedToInProgressCommand)
mediator->>cmdHandler: Handle(StateCommandBase request)
cmdHandler->>cmd: Execute(StateCommandContext)
cmdHandler->>cmdContext: new { CurrentDateTime = UtcNow }
cmd->>cmdBase: base.Execute(context)
cmdBase->>order: ChangeStatus(CurrentUser, date, InProgress)
cmdHandler->>db: Attach/Add or Update(order)
cmdHandler->>db: SaveChangesAsync()
db-->>cmdHandler: persisted
cmdHandler->>result: new StateCommandResult(order, "Begin", debugMessage)
result-->>mediator: return
mediator-->>sBus: StateCommandResult
```
