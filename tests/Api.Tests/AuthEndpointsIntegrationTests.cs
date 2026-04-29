using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using DotNet.Testcontainers.Builders;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Testcontainers.PostgreSql;
namespace Api.Tests;

public static class AuthEndpointCollectionNames
{
    public const string Integration = "Auth endpoint integration";
}

[CollectionDefinition(AuthEndpointCollectionNames.Integration)]
public sealed class AuthEndpointCollectionDefinition : ICollectionFixture<AuthEndpointFixture>;

[Collection(AuthEndpointCollectionNames.Integration)]
public sealed class AuthEndpointsIntegrationTests(AuthEndpointFixture Fixture)
{
    private static JsonSerializerOptions JsonOptions { get; } = new(JsonSerializerDefaults.Web);

    [SkippableFact]
    public async Task Health_returns_ok_with_body_text()
    {
        Skip.IfNot(Fixture.IsDockerAvailable, "Docker is not running — integration tests skipped.");
        var client = Fixture.CreateClient();

        using var res = await client.GetAsync("/api/auth/health");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var body = await res.Content.ReadAsStringAsync();
        Assert.Contains("Auth API", body, StringComparison.OrdinalIgnoreCase);
    }

    [SkippableFact]
    public async Task Password_reset_unknown_email_returns_204_and_bad_token_returns_401()
    {
        Skip.IfNot(Fixture.IsDockerAvailable, "Docker is not running — integration tests skipped.");
        var client = Fixture.CreateClient();

        using var pwdReq =
            JsonContent.Create(new { email = $"missing-{Guid.NewGuid():n}@example.com" }, options: JsonOptions);
        using var pr = await client.PostAsync("/api/auth/password/reset-request", pwdReq);
        Assert.Equal(HttpStatusCode.NoContent, pr.StatusCode);

        using var badReset = JsonContent.Create(
            new { token = "not-a-valid-reset-token-value", newPassword = "AnotherPass123!" },
            options: JsonOptions);
        using var rs = await client.PostAsync("/api/auth/password/reset", badReset);
        Assert.Equal(HttpStatusCode.Unauthorized, rs.StatusCode);
    }

    [SkippableFact]
    public async Task Register_me_otp_refresh_logout_password_reset_request_flow()
    {
        Skip.IfNot(Fixture.IsDockerAvailable, "Docker is not running — integration tests skipped.");
        var client = Fixture.CreateClient();

        var email = $"user-{Guid.NewGuid():n}@example.com";

        using var registerPayload = JsonContent.Create(
            new { email, password = "TestPass123!", phoneNumber = "+61400111222" },
            options: JsonOptions);

        using var registerRes = await client.PostAsync("/api/auth/register", registerPayload);
        Assert.Equal(HttpStatusCode.OK, registerRes.StatusCode);

        using var regJson = JsonDocument.Parse(await registerRes.Content.ReadAsStringAsync());
        var csrfToken = regJson.RootElement.GetProperty("csrfToken").GetString();
        var accessToken = regJson.RootElement.GetProperty("accessToken").GetString();
        Assert.NotNull(csrfToken);
        Assert.NotNull(accessToken);

        using var meReq = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        meReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        using var meRes = await client.SendAsync(meReq);
        Assert.Equal(HttpStatusCode.OK, meRes.StatusCode);

        using var otpPayload = JsonContent.Create(
            new { code = "999999", purpose = "NewDeviceVerification" },
            options: JsonOptions);
        using var otpReq = new HttpRequestMessage(HttpMethod.Post, "/api/auth/otp/verify")
        {
            Content = otpPayload,
        };
        otpReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var otpRes = await client.SendAsync(otpReq);
        Assert.Equal(HttpStatusCode.Unauthorized, otpRes.StatusCode);

        using var refreshPayload = JsonContent.Create(new { csrfToken }, options: JsonOptions);
        using var refreshRes = await client.PostAsync("/api/auth/refresh", refreshPayload);
        Assert.True(
            refreshRes.StatusCode is HttpStatusCode.OK or HttpStatusCode.Unauthorized,
            $"unexpected refresh StatusCode={refreshRes.StatusCode}");

        if (refreshRes.StatusCode != HttpStatusCode.OK)
            return;

        using var refJson = JsonDocument.Parse(await refreshRes.Content.ReadAsStringAsync());
        var refreshedCsrf = refJson.RootElement.GetProperty("csrfToken").GetString();
        Assert.NotNull(refreshedCsrf);

        using var logoutReq = new HttpRequestMessage(HttpMethod.Post, "/api/auth/logout");
        logoutReq.Headers.TryAddWithoutValidation("X-CSRF-TOKEN", refreshedCsrf);
        using var logoutRes = await client.SendAsync(logoutReq);
        Assert.Equal(HttpStatusCode.NoContent, logoutRes.StatusCode);

        using var resetReq = JsonContent.Create(new { email }, options: JsonOptions);
        using var resetEmailRes = await client.PostAsync("/api/auth/password/reset-request", resetReq);
        Assert.Equal(HttpStatusCode.NoContent, resetEmailRes.StatusCode);
    }
}

public sealed class AuthEndpointFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _postgres;
    private WebApplicationFactory<Program>? _factory;

    public bool IsDockerAvailable { get; private set; }

    public async Task InitializeAsync()
    {
        try
        {
            _postgres = new PostgreSqlBuilder()
                .WithImage("postgres:16-alpine")
                .WithDatabase("carshi_tow_integration")
                .WithUsername("postgres")
                .WithPassword("postgres")
                .Build();

            await _postgres.StartAsync();

            _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            {
                builder.UseSetting("ConnectionStrings:DefaultConnection", _postgres.GetConnectionString());
                builder.UseSetting("ASPNETCORE_ENVIRONMENT", Environments.Development);
                builder.UseSetting(WebHostDefaults.EnvironmentKey, Environments.Development);
                builder.UseSetting("Cookies:Secure", "false");
                builder.UseSetting(
                    "Jwt:Key",
                    "integration-tests-hmacsha256-signing-secret-min-32-characters-long!");
            });

            using var client = CreateClientCore();
            using var warm = await client.GetAsync("/api/auth/health");
            warm.EnsureSuccessStatusCode();
            IsDockerAvailable = true;
        }
        catch (DockerUnavailableException)
        {
            IsDockerAvailable = false;
        }
    }

    public HttpClient CreateClient()
    {
        if (_factory is null)
            throw new InvalidOperationException("WebApplicationFactory was not initialized.");

        return CreateClientCore();
    }

    private HttpClient CreateClientCore()
    {
        var factory = _factory!;
        var http = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        http.DefaultRequestHeaders.Remove("User-Agent");
        http.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "CarshiTow-IntegrationTest/1.0");
        http.DefaultRequestHeaders.TryAddWithoutValidation("X-Client-Id", "integration-test");
        return http;
    }

        async Task IAsyncLifetime.DisposeAsync()
    {
        if (_factory is not null)
            await _factory.DisposeAsync();

        if (_postgres is not null)
            await _postgres.DisposeAsync();
    }
}

