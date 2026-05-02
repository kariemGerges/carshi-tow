using System.Threading.Channels;

namespace CarshiTow.Infrastructure.Media;

public sealed class PreviewJobChannel
{
    public Channel<Guid> Work { get; } = System.Threading.Channels.Channel.CreateBounded<Guid>(new BoundedChannelOptions(10_000)
    {
        FullMode = BoundedChannelFullMode.Wait,
        SingleReader = true,
        SingleWriter = false,
    });
}
