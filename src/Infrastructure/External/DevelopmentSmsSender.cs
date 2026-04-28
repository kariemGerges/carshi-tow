using CarshiTow.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace CarshiTow.Infrastructure.External;

public sealed class DevelopmentSmsSender(ILogger<DevelopmentSmsSender> logger) : ISmsSender
{
    public Task SendAsync(string toPhoneNumber, string message, CancellationToken cancellationToken)
    {
        logger.LogInformation("DEV SMS (not sent) To={ToPhoneNumber} Message={Message}", toPhoneNumber, message);
        return Task.CompletedTask;
    }
}
