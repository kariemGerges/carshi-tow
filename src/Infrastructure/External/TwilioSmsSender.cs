using CarshiTow.Application.Configuration;
using CarshiTow.Application.Interfaces;
using Microsoft.Extensions.Options;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace CarshiTow.Infrastructure.External;

public sealed class TwilioSmsSender(IOptions<TwilioSettings> settings) : ISmsSender
{
    private readonly TwilioSettings _settings = settings.Value;

    public async Task SendAsync(string toPhoneNumber, string message, CancellationToken cancellationToken)
    {
        TwilioClient.Init(_settings.AccountSid, _settings.AuthToken);

        await MessageResource.CreateAsync(
            to: new PhoneNumber(toPhoneNumber),
            from: new PhoneNumber(_settings.FromPhoneNumber),
            body: message);
    }
}
