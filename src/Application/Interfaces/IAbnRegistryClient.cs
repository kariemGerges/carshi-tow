namespace CarshiTow.Application.Interfaces;

public interface IAbnRegistryClient
{
    /// <summary>Checksum (+ optional remote lookup when configured). Returns canonical 11-digit ABN.</summary>
    Task<string> RequireValidCanonicalAbnAsync(string abnInput, CancellationToken cancellationToken);
}
