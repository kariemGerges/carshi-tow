using CarshiTow.Application.Interfaces;
using Ganss.Xss;

namespace CarshiTow.Application.Security;

public sealed class InputSanitizer : IInputSanitizer
{
    private readonly HtmlSanitizer _sanitizer = new();

    public string Sanitize(string input) => _sanitizer.Sanitize(input);
}
