# Lab 10: Tracer Bullet Transactions - Proving Distributed Messaging

**Curriculum Section:** Section 05-06 (Team/Process Design / Operate-Execute)
**Estimated Time:** 40 minutes
**Type:** Analyze + Build

---

## Objective

Understand the tracer bullet pattern for proving end-to-end messaging through NServiceBus. A tracer bullet is the simplest possible transaction that traverses the full distributed pipeline, proving the infrastructure works before building features on top of it.

---

## Context

The application uses NServiceBus with SQL Server Transport for asynchronous messaging between the UI Server and the Worker process. A **tracer bullet** is a minimal message that proves the full Send/Reply pipeline works — like firing a tracer round to see where the bullet goes before loading live ammunition.

```
Test Endpoint --Send(TracerBulletCommand)--> SQL Transport --> Worker Endpoint
Worker Endpoint --Reply(TracerBulletReplyMessage)--> SQL Transport --> Test Endpoint
```

---

## Steps

### Step 1: Read the Tracer Bullet Command

Open `src/Core/Model/Messages/TracerBulletCommand.cs`:

```csharp
public record TracerBulletCommand(Guid CorrelationId);
```

This is the simplest possible message — just a correlation ID. No business logic, no side effects. Its only purpose is to prove the pipeline works.

### Step 2: Read the Worker Handler

Open `src/Worker/Handlers/TracerBulletHandler.cs`:

```csharp
public async Task Handle(TracerBulletCommand message, IMessageHandlerContext context)
{
    _logger.LogInformation("TracerBullet received: {CorrelationId}. Sending reply.", message.CorrelationId);
    await context.Reply(new TracerBulletReplyMessage(message.CorrelationId));
}
```

The handler logs receipt and replies — nothing else. It proves the Worker endpoint is alive and processing messages.

### Step 3: Read the Signal Mechanism

Open `src/IntegrationTests/TracerBulletSignal.cs`. Study the `ConcurrentDictionary<Guid, TaskCompletionSource<bool>>` pattern:

- `WaitForReply(correlationId, timeout)` — registers a signal and blocks until completed or timed out
- `Complete(correlationId)` — called by the reply handler when the reply arrives

This is a thread-safe coordination mechanism for asynchronous messaging tests.

### Step 4: Read the Reply Handler

Open `src/IntegrationTests/Handlers/TracerBulletReplyHandler.cs`. It calls `TracerBulletSignal.Complete(correlationId)` to unblock the waiting test.

### Step 5: Study the Acceptance Test

Open `src/AcceptanceTests/NServiceBus/TracerBulletTests.cs`:

```csharp
[Test]
public async Task TracerBullet_WorkerReceivesCommandAndReplies()
{
    if (!ServerFixture.WorkerStarted)
        Assert.Ignore("Worker is not running (requires SqlServerTransport).");

    var correlationId = Guid.NewGuid();
    var messageSession = TestHost.GetRequiredService<IMessageSession>(newScope: false);
    var replyTask = TracerBulletSignal.WaitForReply(correlationId, TimeSpan.FromSeconds(60));

    await messageSession.Send(new TracerBulletCommand(correlationId));
    await replyTask;
}
```

Key patterns:
- `RequiresBrowser => false` — no browser needed for messaging tests
- Skips gracefully if Worker is not running
- Registers the signal **before** sending (avoids race condition)
- Uses correlation ID to match request with reply

### Step 6: Draw the Message Flow

On paper, draw the full message flow including:
1. Test registers signal in `TracerBulletSignal`
2. Test sends `TracerBulletCommand` via `IMessageSession`
3. SQL Server Transport picks up the message
4. Worker's `TracerBulletHandler` receives and replies
5. SQL Server Transport routes reply back
6. Test's `TracerBulletReplyHandler` receives reply
7. Handler calls `TracerBulletSignal.Complete()`
8. Test's `WaitForReply` unblocks — test passes

### Step 7: Think About New Tracer Bullets

**Design exercise:** If the application added a new messaging endpoint (e.g., a Notification Service), what would its tracer bullet look like?

- What new command/reply records would you create?
- Where would the handler live?
- What would the acceptance test look like?

---

## Expected Outcome

- Understanding of the tracer bullet pattern for distributed systems
- Ability to trace the full NServiceBus Send/Reply pipeline
- Understanding of thread-safe signaling with `TaskCompletionSource`

---

## Discussion Questions

1. Why is the tracer bullet the **first** thing built when adding distributed messaging? What does it prove before any feature code exists?
2. The test skips gracefully when the Worker is not running. Why is this important for CI environments that may not have SQL Server Transport?
3. How does the `ConcurrentDictionary<Guid, TaskCompletionSource<bool>>` pattern handle parallel test execution?
4. The tracer bullet has zero business logic. Why is this a feature, not a limitation?
5. How does this relate to the "Achieving Stability" pillar? Could tracer bullets run continuously in production as health probes?
