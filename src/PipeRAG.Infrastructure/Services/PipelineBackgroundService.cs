using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PipeRAG.Core.Interfaces;

namespace PipeRAG.Infrastructure.Services;

/// <summary>
/// Background service that processes pipeline runs from the channel queue.
/// </summary>
public class PipelineBackgroundService : BackgroundService
{
    private readonly PipelineRunChannel _channel;
    private readonly IAutoPipelineService _pipelineService;
    private readonly ILogger<PipelineBackgroundService> _logger;

    public PipelineBackgroundService(
        PipelineRunChannel channel,
        IAutoPipelineService pipelineService,
        ILogger<PipelineBackgroundService> logger)
    {
        _channel = channel;
        _pipelineService = pipelineService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Pipeline background service started");

        await foreach (var runId in _channel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                _logger.LogInformation("Processing pipeline run {RunId}", runId);
                await _pipelineService.ExecutePipelineRunAsync(runId, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error processing pipeline run {RunId}", runId);
            }
        }
    }
}
