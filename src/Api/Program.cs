using System.Diagnostics;
using CarshiTow.Application.Configuration;
using CarshiTow.Infrastructure.Seeding;
using CarshiTow.Api.Authorization;
using CarshiTow.Api.ExceptionHandling;
using CarshiTow.Api.Extensions;
using CarshiTow.Api.Filters;
using CarshiTow.Api.Middleware;
using CarshiTow.Infrastructure.Persistence;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));
builder.Services.Configure<TwilioSettings>(builder.Configuration.GetSection(TwilioSettings.SectionName));
builder.Services.Configure<CookieSettings>(builder.Configuration.GetSection(CookieSettings.SectionName));
builder.Services.Configure<RedisSecuritySettings>(builder.Configuration.GetSection(RedisSecuritySettings.SectionName));
builder.Services.Configure<PasswordResetSettings>(builder.Configuration.GetSection(PasswordResetSettings.SectionName));
builder.Services.Configure<CrashifySeedSettings>(builder.Configuration.GetSection(CrashifySeedSettings.SectionName));
builder.Services.Configure<PublicLinksSettings>(builder.Configuration.GetSection(PublicLinksSettings.SectionName));
builder.Services.Configure<ObjectStorageSettings>(builder.Configuration.GetSection(ObjectStorageSettings.SectionName));
builder.Services.Configure<AbnLookupSettings>(builder.Configuration.GetSection(AbnLookupSettings.SectionName));

builder.Services.AddControllers(options =>
{
    options.Filters.Add<ValidationFilter>();
    options.Filters.Add<DeviceFingerprintFilter>();
    options.Filters.Add<SrsApiEnvelopeFilter>();
}).AddJsonOptions(o =>
{
    o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddMemoryCache();
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidation();
builder.Services.AddHttpContextAccessor();
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration, builder.Environment);
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddAuthorization(o => CarshiTowAuthorizationPolicies.AddPolicies(o));
builder.Services.AddApiRateLimiting();
builder.Services.Configure<ApiBehaviorOptions>(o => o.SuppressModelStateInvalidFilter = true);

builder.Services.AddProblemDetails(o =>
{
    o.CustomizeProblemDetails = ctx =>
    {
        var http = ctx.HttpContext;
        if (http.Items.TryGetValue(CorrelationIdMiddleware.ItemsKey, out var v) && v is string correlationId)
        {
            ctx.ProblemDetails.Extensions.TryAdd("correlationId", correlationId);
        }

        ctx.ProblemDetails.Extensions.TryAdd("traceId", Activity.Current?.Id ?? http.TraceIdentifier);
    };
});

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

{
    using var scope = app.Services.CreateScope();
    await CrashifyAdminSeed.RunAsync(scope.ServiceProvider, CancellationToken.None);
}

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseExceptionHandler();
app.UseSecureHeaders();
app.UseMiddleware<DeviceFingerprintMiddleware>();
app.UseMiddleware<JwtMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.Run();

public partial class Program;
