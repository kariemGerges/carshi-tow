using CarshiTow.Api.DTOs;

namespace CarshiTow.Api.Mappings;

public static class AuthMappingExtensions
{
    public static DeviceFingerprintDto ToDeviceFingerprintDto(this HttpContext context) =>
        new(context.Items["DeviceFingerprint"]?.ToString() ?? string.Empty);
}
