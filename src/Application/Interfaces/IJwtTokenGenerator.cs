namespace CarshiTow.Application.Interfaces;

public interface IJwtTokenGenerator
{
    string Generate(Guid userId, string email);
}
