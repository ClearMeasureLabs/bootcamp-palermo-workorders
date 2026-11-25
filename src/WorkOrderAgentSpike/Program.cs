// WorkOrderAgentSpike — minimal ACP v1 LibAcp stdio spike agent host.
// All logging goes to stderr; this process never writes to stdout.
using ClearMeasure.Bootcamp.AgentProtocols.LibAcp;
using ClearMeasure.Bootcamp.WorkOrderAgentSpike;

Console.Error.WriteLine("WorkOrderAgentSpike starting (ACP v1 stdio spike agent).");

await using var connection = LibAcpAgentConnection.CreateStdio(() => new WorkOrderSpikeAgentHandler(new McpWorkOrderClient()));
await connection.Closed;
