using System.Diagnostics;
using Azure;
using Azure.AI.OpenAI;
using ClearMeasure.Bootcamp.AcceptanceTests;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.IntegrationTests;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using ModelContextProtocol.Client;
using Shouldly;

namespace ClearMeasure.Bootcamp.McpAcceptanceTests;

[TestFixture]
public class McpChatConversationTests
{
	private const string McpServerRelativeProjectDir = "../../../../McpServer";
	private const int HttpPort = 5198;

	private static string BuildConfiguration =>
		AppDomain.CurrentDomain.BaseDirectory.Contains(
			Path.DirectorySeparatorChar + "Release" + Path.DirectorySeparatorChar)
			? "Release"
			: "Debug";

	private McpClient? _mcpClient;
	private IList<McpClientTool>? _tools;
	private Process? _serverProcess;

	[SetUp]
	public async Task SetUp()
	{
		ServerFixture.InitializeDatabaseOnce();

		var connectionString = ResolveConnectionString();
		EnableSqliteWalMode(connectionString);
		TestHost.GetRequiredService<IDatabaseConfiguration>().ResetConnectionPool();

		await StartHttpMcpServer(connectionString);
		CheckLlmAvailability();
	}

	[TearDown]
	public async Task TearDown()
	{
		if (_mcpClient != null)
		{
			await _mcpClient.DisposeAsync();
			_mcpClient = null;
		}

		if (_serverProcess != null && !_serverProcess.HasExited)
		{
			_serverProcess.Kill(entireProcessTree: true);
			_serverProcess.Dispose();
			_serverProcess = null;
		}
	}

	[Test, Retry(2)]
	public async Task ShouldCreateAndAssignWorkOrderFromConversationalPrompt()
	{
		var response = await SendPrompt(
			"I am Timothy Lovejoy (my username is tlovejoy). " +
			"Create a new work order assigned to Groundskeeper Willie (username gwillie) " +
			"to cut the grass and make sure that the edging is done and that fertilizer is put down. " +
			"This will be on the outdoor lawn. " +
			"Steps to follow:\n" +
			"1. Call create-work-order with a suitable title, a description that captures the full scope of work " +
			"(cutting grass, edging, and fertilizer), creatorUsername='tlovejoy', and roomNumber='Outdoor Lawn'.\n" +
			"2. Take the work order Number returned from step 1 and call execute-work-order-command with " +
			"commandName='DraftToAssignedCommand', executingUsername='tlovejoy', assigneeUsername='gwillie'.\n" +
			"Report the final work order details including the number, title, description, status, assignee, and room number.");

		response.Text.ShouldNotBeNullOrEmpty();

		var bus = TestHost.GetRequiredService<IBus>();
		var workOrders = await bus.Send(new WorkOrderSpecificationQuery());

		var lawnWorkOrder = workOrders.FirstOrDefault(wo =>
			wo.Assignee?.UserName == "gwillie" &&
			wo.Status == WorkOrderStatus.Assigned &&
			wo.Creator?.UserName == "tlovejoy" &&
			(wo.Title!.Contains("grass", StringComparison.OrdinalIgnoreCase) ||
			 wo.Title!.Contains("lawn", StringComparison.OrdinalIgnoreCase) ||
			 wo.Description!.Contains("grass", StringComparison.OrdinalIgnoreCase)));

		lawnWorkOrder.ShouldNotBeNull(
			"Expected a work order for lawn care created by tlovejoy and assigned to gwillie");
		lawnWorkOrder.Status.ShouldBe(WorkOrderStatus.Assigned);
		lawnWorkOrder.Creator!.UserName.ShouldBe("tlovejoy");
		lawnWorkOrder.Assignee!.UserName.ShouldBe("gwillie");
		lawnWorkOrder.Title.ShouldNotBeNullOrEmpty();
		lawnWorkOrder.Description.ShouldNotBeNullOrEmpty();
		var description = lawnWorkOrder.Description!.ToLowerInvariant();
		description.ShouldContain("grass");
		(description.Contains("edging") || description.Contains("edge"))
			.ShouldBeTrue($"Expected description to mention edging or edges: {lawnWorkOrder.Description}");
		description.ShouldContain("fertilizer");
		lawnWorkOrder.RoomNumber.ShouldNotBeNullOrEmpty(
			"Room number should be set to a value representing the outdoor lawn");
	}

	private async Task StartHttpMcpServer(string connectionString)
	{
		var mcpServerProjectDir = Path.GetFullPath(
			Path.Combine(AppDomain.CurrentDomain.BaseDirectory, McpServerRelativeProjectDir));

		TestContext.Out.WriteLine($"McpChatConversationTests: building MCP server at {mcpServerProjectDir}");

		var buildProcess = Process.Start(new ProcessStartInfo
		{
			FileName = "dotnet",
			Arguments = $"build \"{mcpServerProjectDir}\" --configuration {BuildConfiguration}",
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false,
			CreateNoWindow = true
		});

		if (buildProcess != null)
		{
			await buildProcess.WaitForExitAsync();
			if (buildProcess.ExitCode != 0)
			{
				var stderr = await buildProcess.StandardError.ReadToEndAsync();
				Assert.Inconclusive($"MCP server build failed: {stderr}");
			}
		}

		TestContext.Out.WriteLine(
			$"McpChatConversationTests: starting MCP server via HTTP on port {HttpPort}");

		_serverProcess = Process.Start(new ProcessStartInfo
		{
			FileName = "dotnet",
			Arguments = $"run --no-build --configuration {BuildConfiguration} --project \"{mcpServerProjectDir}\" -- --transport http",
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false,
			CreateNoWindow = true,
			Environment =
			{
				["ConnectionStrings__SqlConnectionString"] = connectionString,
				["ASPNETCORE_URLS"] = $"http://localhost:{HttpPort}"
			}
		});

		if (_serverProcess == null || _serverProcess.HasExited)
		{
			Assert.Inconclusive("Failed to start MCP HTTP server process");
			return;
		}

		_serverProcess.ErrorDataReceived += (_, e) =>
		{
			if (e.Data != null)
				TestContext.Out.WriteLine($"[McpServer stderr] {e.Data}");
		};
		_serverProcess.BeginErrorReadLine();

		await WaitForServerReady();

		var transport = new HttpClientTransport(new HttpClientTransportOptions
		{
			Endpoint = new Uri($"http://localhost:{HttpPort}"),
			Name = "ChurchBulletin"
		});

		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
		_mcpClient = await McpClient.CreateAsync(transport, cancellationToken: cts.Token);
		_tools = await _mcpClient.ListToolsAsync(cancellationToken: cts.Token);

		TestContext.Out.WriteLine(
			$"McpChatConversationTests: connected via HTTP, {_tools.Count} tools discovered");
	}

	private async Task WaitForServerReady()
	{
		using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
		var deadline = DateTime.UtcNow.AddSeconds(30);

		while (DateTime.UtcNow < deadline)
		{
			try
			{
				var response = await httpClient.PostAsync(
					$"http://localhost:{HttpPort}",
					new StringContent("{}", System.Text.Encoding.UTF8, "application/json"));
				TestContext.Out.WriteLine(
					$"McpChatConversationTests: HTTP server responded with {(int)response.StatusCode}");
				return;
			}
			catch
			{
				// Server not ready yet
			}

			await Task.Delay(500);
		}

		Assert.Inconclusive("MCP HTTP server did not become ready within 30 seconds");
	}

	private async Task<ChatResponse> SendPrompt(string prompt)
	{
		var chatClient = BuildChatClient();
		var messages = new List<ChatMessage>
		{
			new(ChatRole.System,
				"You are a helpful assistant with access to tools for managing work orders and employees. " +
				"Always use the provided tools to answer questions. Return the raw data from tool results."),
			new(ChatRole.User, prompt)
		};

		using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(2));
		return await chatClient.GetResponseAsync(messages,
			new ChatOptions { Tools = [.. _tools!] },
			cts.Token);
	}

	private static IChatClient BuildChatClient()
	{
		var apiKey = McpServerFixture.GetLlmConfigValue("AI_OpenAI_ApiKey");
		if (!string.IsNullOrEmpty(apiKey))
		{
			var url = McpServerFixture.GetLlmConfigValue("AI_OpenAI_Url")
					  ?? throw new InvalidOperationException("AI_OpenAI_Url is required");
			var model = McpServerFixture.GetLlmConfigValue("AI_OpenAI_Model")
						?? throw new InvalidOperationException("AI_OpenAI_Model is required");

			var credential = new AzureKeyCredential(apiKey);
			var openAiClient = new AzureOpenAIClient(new Uri(url), credential);
			return openAiClient.GetChatClient(model).AsIChatClient()
				.AsBuilder()
				.UseFunctionInvocation()
				.Build();
		}

		Assert.Inconclusive("No LLM available (set AI_OpenAI_ApiKey/Url/Model or run Ollama locally)");
		throw new InvalidOperationException("Unreachable");
	}

	private static void CheckLlmAvailability()
	{
		var apiKey = McpServerFixture.GetLlmConfigValue("AI_OpenAI_ApiKey");
		if (string.IsNullOrEmpty(apiKey))
			Assert.Inconclusive("No LLM available (set AI_OpenAI_ApiKey/Url/Model)");
	}

	private static string ResolveConnectionString()
	{
		var configuration = TestHost.GetRequiredService<IConfiguration>();
		var connectionString = configuration.GetConnectionString("SqlConnectionString") ?? "";

		if (connectionString.StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase)
			&& !connectionString.Contains(":memory:", StringComparison.OrdinalIgnoreCase))
		{
			var dbPath = connectionString["Data Source=".Length..].Trim();
			var semicolonIndex = dbPath.IndexOf(';');
			if (semicolonIndex >= 0) dbPath = dbPath[..semicolonIndex];

			if (!Path.IsPathRooted(dbPath))
			{
				var absolutePath = Path.GetFullPath(dbPath);
				connectionString = $"Data Source={absolutePath}";
			}
		}

		return connectionString;
	}

	private static void EnableSqliteWalMode(string connectionString)
	{
		if (!connectionString.StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase))
			return;

		try
		{
			using var connection = new Microsoft.Data.Sqlite.SqliteConnection(connectionString);
			connection.Open();
			using var command = connection.CreateCommand();
			command.CommandText = "PRAGMA journal_mode=WAL;";
			command.ExecuteScalar();
		}
		catch
		{
			// Best effort
		}
	}
}
