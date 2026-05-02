namespace CarshiTow.Api.Srs;

public sealed record SrsEnvelopeError(
    string Code,
    string Message,
    string? CorrelationId,
    IReadOnlyDictionary<string, string[]>? Fields);

public sealed record SrsEnvelope(bool Success, object? Data, SrsEnvelopeError? Error);
