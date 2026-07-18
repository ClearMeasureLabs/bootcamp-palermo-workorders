using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.UI.Server;

/// <summary>
///     Background service that periodically evaluates work requests and reformats
///     their title and description fields using an AI agent.
///     Title is corrected to start with a capital letter.
///     Description is corrected for grammar and punctuation.
/// </summary>
public class AutoReformatAgentService : BackgroundService
{
    private readonly IServiceScope _serviceScope;
    private readonly ILogger<AutoReformatAgentService> _logger;
    private readonly IConfiguration _configuration;
    private readonly TimeProvider _timeProvider;

    public AutoReformatAgentService(
        IServiceProvider serviceProvider,
        ILogger<AutoReformatAgentService> logger,
        IConfiguration configuration,
        TimeProvider timeProvider)
    {
        _serviceScope = serviceProvider.CreateScope();
        _logger = logger;
        _configuration = configuration;
        _timeProvider = timeProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (string.Equals(_configuration["DISABLE_AUTO_REFORMAT_AGENT"], "true",
                StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("AutoReformatAgentService disabled via DISABLE_AUTO_REFORMAT_AGENT configuration");
            return;
        }

        _logger.LogInformation("AutoReformatAgentService started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ReformatWorkRequestsAsync();
                await Task.Delay(TimeSpan.FromSeconds(5), _timeProvider, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AutoReformatAgentService execution");
                await Task.Delay(TimeSpan.FromSeconds(30), _timeProvider, stoppingToken);
            }
        }

        _logger.LogInformation("AutoReformatAgentService stopped");
    }

    private async Task ReformatWorkRequestsAsync()
    {
        var bus = _serviceScope.ServiceProvider.GetRequiredService<IBus>();
        var agent = _serviceScope.ServiceProvider.GetRequiredService<WorkRequestReformatAgent>();

        try
        {
            var specification = new WorkRequestSpecificationQuery();
            specification.MatchStatus(WorkRequestStatus.Draft);

            var draftWorkRequests = await bus.Send(specification);

            _logger.LogDebug("Found {Count} draft work requests to evaluate for reformatting", draftWorkRequests.Length);

            foreach (var workRequest in draftWorkRequests)
            {
                try
                {
                    var result = await agent.ReformatWorkRequestAsync(workRequest);

                    if (result != null)
                    {
                        await ApplyReformatAsync(workRequest, result);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error reformatting WorkRequest {WorkRequestNumber}",
                        workRequest.Number);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving draft work requests for reformatting");
        }
    }

    private async Task ApplyReformatAsync(WorkRequest workRequest, ReformatResult result)
    {
        using var scope = _serviceScope.ServiceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DbContext>();

        try
        {
            workRequest.Title = result.Title;
            workRequest.Description = result.Description;

            dbContext.Attach(workRequest);
            dbContext.Update(workRequest);
            await dbContext.SaveChangesAsync();

            _logger.LogInformation("Successfully reformatted WorkRequest {WorkRequestNumber}",
                workRequest.Number);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving reformatted WorkRequest {WorkRequestNumber}",
                workRequest.Number);
        }
    }
}
