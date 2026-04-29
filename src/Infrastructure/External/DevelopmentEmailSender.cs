using CarshiTow.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace CarshiTow.Infrastructure.External;

public sealed class DevelopmentEmailSender(ILogger<DevelopmentEmailSender> logger) : IEmailSender
{
    public Task SendAsync(string toEmail, string subject, string body, CancellationToken cancellationToken)
    {
        logger.LogInformation("[Email dev] To={To}, Subject={Subject}\n{Body}", toEmail, subject, body);
        return Task.CompletedTask;
    }
}
