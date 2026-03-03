# WorkOrderChatHandler Sequence Diagram

This diagram shows the complete flow of a user chat interaction with the AI assistant on a Work Order, from the Blazor WASM client through the server-side `WorkOrderChatHandler` to Azure OpenAI and back.

## Key Components

| Component | Description |
|-----------|-------------|
| **WorkOrderChat.razor** | Blazor component providing the chat UI for work order AI assistance |
| **RemotableBus** | Client-side bus that routes `IRemotableRequest` messages to the server |
| **PublisherGateway** | HTTP client that serializes and sends requests to `SingleApiController` |
| **SingleApiController** | Server-side API endpoint that deserializes and dispatches messages |
| **WorkOrderChatHandler** | MediatR handler that orchestrates the LLM conversation |
| **ChatClientFactory** | Factory that creates and configures the Azure OpenAI chat client |
| **TracingChatClient** | Decorator that adds distributed tracing to chat operations |
| **WorkOrderTool** | AI function tool that allows the LLM to query work order data |

## Flow Overview

1. **User Input**: User types a prompt in the chat input and clicks Send
2. **Client Transport**: `WorkOrderChatQuery` is wrapped in `WebServiceMessage` and sent via HTTP POST
3. **Server Dispatch**: `SingleApiController` deserializes and routes to `WorkOrderChatHandler` via MediatR
4. **LLM Setup**: Handler builds system prompts with work order context and configures AI function tools
5. **LLM Call**: Request sent to Azure OpenAI with function calling enabled
6. **Tool Invocation**: If LLM needs work order details, it calls `GetWorkOrderByNumber` tool
7. **Response Return**: Final response flows back through the same pipeline to the UI

## PlantUML Diagram

```plantuml
@startuml

title WorkOrderChatHandler Sequence Diagram

actor "User" as user
participant "WorkOrderChat.razor\n(Blazor WASM)" as ui
participant "RemotableBus" as rBus
participant "PublisherGateway" as gateway
participant "SingleApiController\n(UI.Server)" as api
participant "Bus : IBus" as sBus
participant "IMediator" as mediator
participant "WorkOrderChatHandler" as handler
participant "ChatClientFactory" as factory
participant "TracingChatClient" as tracingClient
participant "IChatClient\n(Azure OpenAI)" as chatClient
participant "WorkOrderTool" as tool
database "DataContext\n(EF Core)" as db

autonumber

== User Sends Chat Message ==

user -> ui : Enter prompt & click Send
ui -> ui : Add user message to\n_chatMessages list
ui -> ui : Set _isLoading = true
ui -> ui : Create WorkOrderChatQuery\n(prompt, currentWorkOrder)
ui -> rBus : Bus.Send(WorkOrderChatQuery)
rBus -> rBus : Check if IRemotableRequest
rBus -> gateway : Publish(remotableRequest)
gateway -> gateway : Wrap in WebServiceMessage
gateway -> api : POST api/blazor-wasm-single-api\n(WebServiceMessage JSON)

== Server-Side Processing ==

api -> api : Deserialize\nWebServiceMessage.GetBodyObject()
api -> sBus : Send(WorkOrderChatQuery)
sBus -> mediator : Send(WorkOrderChatQuery)
mediator -> handler : Handle(WorkOrderChatQuery,\nCancellationToken)

== Build Chat Messages ==

handler -> handler : Extract prompt from request
handler -> handler : Create List<ChatMessage>\n- System: "You help users do work..."\n- System: "Work Order number is {Number}"\n- System: "Limit to 3 sentences..."\n- User: prompt

== Get Chat Client ==

handler -> factory : GetChatClient()
factory -> factory : Send ChatClientConfigQuery\nvia IBus
factory -> factory : Build AzureOpenAIClient\nwith credentials
factory -> factory : Get ChatClient for model
factory -> factory : AsIChatClient()\n.UseFunctionInvocation()
factory -> tracingClient : new TracingChatClient(innerClient)
factory --> handler : IChatClient (wrapped)

== Configure Tools ==

handler -> handler : Create ChatOptions\nwith Tools = [GetWorkOrderByNumber]
note right of handler
  AIFunctionFactory.Create wraps
  workOrderTool.GetWorkOrderByNumber
  as an AI function
end note

== Call LLM ==

handler -> tracingClient : GetResponseAsync(\nchatMessages, chatOptions)
tracingClient -> tracingClient : StartActivity\n("ChatClient.GetResponseAsync")
tracingClient -> chatClient : GetResponseAsync(\nmessages, options)
chatClient -> chatClient : Send request to\nAzure OpenAI API

alt LLM Requests Tool Call
  chatClient --> chatClient : Response includes\nfunction_call for GetWorkOrderByNumber
  chatClient -> tool : GetWorkOrderByNumber(workOrderNumber)
  tool -> db : WorkOrderByNumberQuery
  db --> tool : WorkOrder entity
  tool --> chatClient : WorkOrder data
  chatClient -> chatClient : Continue conversation\nwith tool result
  chatClient --> tracingClient : Final ChatResponse
else LLM Returns Direct Response
  chatClient --> tracingClient : ChatResponse
end

tracingClient -> tracingClient : SetTag("chat.response", ...)
tracingClient --> handler : ChatResponse

== Return Response ==

handler --> mediator : ChatResponse
mediator --> sBus : ChatResponse
sBus --> api : ChatResponse
api -> api : Wrap in WebServiceMessage
api --> gateway : WebServiceMessage JSON
gateway -> gateway : Deserialize response
gateway --> rBus : WebServiceMessage
rBus -> rBus : Extract ChatResponse\nfrom GetBodyObject()
rBus --> ui : ChatResponse

== Update UI ==

ui -> ui : Extract response.Text
ui -> ui : Add AI message to\n_chatMessages list
ui -> ui : Set _isLoading = false
ui -> ui : StateHasChanged()
ui --> user : Display AI response\nin chat history

@enduml
```

## Related Files

- `src/UI.Shared/Components/WorkOrderChat.razor` - Chat UI component
- `src/LlmGateway/WorkOrderChatHandler.cs` - MediatR request handler
- `src/LlmGateway/WorkOrderChatQuery.cs` - Request/query record
- `src/LlmGateway/WorkOrderTool.cs` - AI function tool for work order queries
- `src/LlmGateway/ChatClientFactory.cs` - Azure OpenAI client factory
- `src/LlmGateway/TracingChatClient.cs` - Tracing decorator for chat client
- `src/UI/Client/RemotableBus.cs` - Client-side remotable bus
- `src/UI/Client/PublisherGateway.cs` - HTTP transport for remotable messages
- `src/UI/Server/Controllers/SingleApiController.cs` - Server-side API endpoint
