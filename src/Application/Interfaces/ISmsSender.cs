namespace CarshiTow.Application.Interfaces;

public interface ISmsSender
{
    Task SendAsync(string toPhoneNumber, string message, CancellationToken cancellationToken);
}
