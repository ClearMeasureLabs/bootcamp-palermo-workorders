## ADDED Requirements

### Requirement: AI Agent chat page is reachable at a dedicated route

The application SHALL expose a full-page AI Agent chat at a dedicated route (e.g. `/ai-agent`). The page SHALL be a standalone Blazor page with no selected work order context.

#### Scenario: User navigates to AI Agent page

- **WHEN** an authenticated user navigates to the AI Agent route (e.g. `/ai-agent`)
- **THEN** the full-page chat UI is displayed with an input, send control, and area for message history

#### Scenario: AI Agent link in navigation

- **WHEN** the user is authenticated
- **THEN** the navigation menu SHALL include a link to the AI Agent chat page

---

### Requirement: User can send messages and receive AI responses

The AI Agent page SHALL allow the user to type a message and send it; the system SHALL call the backend with the prompt and SHALL display the AI response (or an error) in the chat history.

#### Scenario: Send message and receive response

- **WHEN** the user enters text and triggers send (e.g. clicks Send)
- **THEN** the user message is appended to the visible chat history and a loading state is shown until the response is received, then the AI response (or error message) is appended to the history

#### Scenario: Send is disabled while loading

- **WHEN** a request is in progress (loading)
- **THEN** sending another message SHALL be prevented or ignored until the current request completes

#### Scenario: Enter key submits message

- **WHEN** the user presses Enter in the message input (without Shift)
- **THEN** the message SHALL be submitted the same as if the user clicked the Send button

---

### Requirement: Chat layout uses full height with input always visible

The chat UI SHALL use the full height of the viewport. The input area SHALL remain visible at the bottom without the user scrolling the browser; only the message history area SHALL scroll.

#### Scenario: Input visible without page scroll

- **WHEN** the user is on the AI Agent page
- **THEN** the input box and Send button SHALL be visible at the bottom of the viewport without the user having to scroll down in the browser

#### Scenario: Message history scrolls

- **WHEN** there are many messages
- **THEN** the message history area SHALL scroll internally and SHALL NOT push the input off-screen or require browser-level scrolling to reach the input

---

### Requirement: Backend uses ChatClientFactory and WorkOrderTool

The backend SHALL handle AI Agent chat with a prompt-only query (no work order). The handler SHALL obtain an chat client from ChatClientFactory and SHALL use the same tools as work-order chat (e.g. WorkOrderTool) with a general system prompt.

#### Scenario: Handler uses ChatClientFactory

- **WHEN** the backend receives an Agent chat request (e.g. AgentChatQuery)
- **THEN** the handler SHALL call ChatClientFactory to get an IChatClient and SHALL use it to generate the response

#### Scenario: No current work order in context

- **WHEN** the handler builds the conversation for the LLM
- **THEN** the system prompt SHALL NOT include a “current work order” and SHALL describe general help with work orders and available tools (e.g. look up work order by number, list employees)

---

### Requirement: Chat is aware of the logged-in user

The AI Agent chat SHALL be aware of the logged-in user. The page SHALL send the current user's identity with each request, and the backend SHALL include that identity in the conversation context so the AI can respond in a user-aware way.

#### Scenario: Page sends current user with request

- **WHEN** the user sends a message from the AI Agent page
- **THEN** the page SHALL obtain the current user (e.g. via UserSession.GetCurrentUserAsync()) and SHALL include the user's identity (e.g. UserName) in the request (e.g. AgentChatQuery)

#### Scenario: Handler includes user in system prompt

- **WHEN** the backend handles an Agent chat request that includes user identity
- **THEN** the handler SHALL include the logged-in user (e.g. username or display name) in the system prompt sent to the LLM so the AI is aware of who is chatting
