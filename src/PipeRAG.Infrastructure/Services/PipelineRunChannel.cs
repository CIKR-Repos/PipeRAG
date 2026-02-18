using System.Threading.Channels;

namespace PipeRAG.Infrastructure.Services;

/// <summary>
/// A bounded channel for queuing pipeline run IDs for background processing.
/// </summary>
public class PipelineRunChannel
{
    private readonly Channel<Guid> _channel = Channel.CreateBounded<Guid>(new BoundedChannelOptions(100)
    {
        FullMode = BoundedChannelFullMode.Wait
    });

    /// <summary>Channel writer for producers (API endpoints).</summary>
    public ChannelWriter<Guid> Writer => _channel.Writer;

    /// <summary>Channel reader for consumers (background service).</summary>
    public ChannelReader<Guid> Reader => _channel.Reader;
}
