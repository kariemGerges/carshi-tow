using CarshiTow.Application.Interfaces;

namespace CarshiTow.Infrastructure.Media;

public sealed class PreviewJobQueueWriter(PreviewJobChannel holder) : IPreviewJobQueue
{
    public void TryEnqueue(Guid photoId) => holder.Work.Writer.TryWrite(photoId);
}
