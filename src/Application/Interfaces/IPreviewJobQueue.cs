namespace CarshiTow.Application.Interfaces;

/// <summary>Queues photos for server-side watermarked preview generation (SRS §IN-003, §8.2).</summary>
public interface IPreviewJobQueue
{
    void TryEnqueue(Guid photoId);
}
