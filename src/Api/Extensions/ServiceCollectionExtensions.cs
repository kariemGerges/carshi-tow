using System.Security.Claims;
using System.Text;
using CarshiTow.Api.Filters;
using CarshiTow.Application.Configuration;
using CarshiTow.Application.Interfaces;
using CarshiTow.Application.Security;
using CarshiTow.Api.Authorization;
using CarshiTow.Application.Services;
using CarshiTow.Application.Validators;
using CarshiTow.Infrastructure.External;
using CarshiTow.Infrastructure;
using CarshiTow.Infrastructure.Persistence;
using CarshiTow.Infrastructure.Repositories;
using CarshiTow.Infrastructure.Security;
using CarshiTow.Infrastructure.Services;
using CarshiTow.Infrastructure.Storage;
using FluentValidation;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace CarshiTow.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IPhotoPackService, PhotoPackService>();
        services.AddScoped<IPackPhotoManagementService, PackPhotoManagementService>();
        services.AddScoped<IPlatformAdminService, PlatformAdminService>();
        services.AddScoped<IAuthorizationHandler, MandatoryMfaEnrollmentHandler>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();
        services.AddScoped<IOtpService, OtpService>();
        services.AddScoped<IDeviceService, DeviceService>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IDeviceFingerprintGenerator, DeviceFingerprintGenerator>();
        services.AddSingleton<IInputSanitizer, InputSanitizer>();
        services.AddSingleton<ICsrfProtectionService, CsrfProtectionService>();
        services.AddSingleton<IBruteForceProtectionService, BruteForceProtectionService>();
        return services;
    }

    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        var redisSettings = configuration.GetSection(RedisSecuritySettings.SectionName).Get<RedisSecuritySettings>() ?? new RedisSecuritySettings();
        if (redisSettings.Enabled)
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisSettings.Configuration;
                options.InstanceName = redisSettings.InstanceName;
            });
        }
        else
        {
            services.AddDistributedMemoryCache();
        }

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IPhotoPackRepository, PhotoPackRepository>();
        services.AddScoped<ITowYardPartyResolver, TowYardPartyResolver>();
        services.AddScoped<IAuditLogWriter, AuditLogWriter>();
        services.AddScoped<IPublicPackLinkService, PublicPackLinkService>();
        services.AddScoped<ITowYardRepository, TowYardRepository>();
        services.AddScoped<IAbnRegistryClient, AbnRegistryClient>();
        services.AddSingleton<IFilePreviewUrlIssuer, DevelopmentFilePreviewUrlIssuer>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();
        services.AddSingleton<IEmailSender, DevelopmentEmailSender>();
        services.AddScoped<IDeviceRepository, DeviceRepository>();
        services.AddScoped<IOtpCodeRepository, OtpCodeRepository>();
        if (environment.IsDevelopment())
        {
            services.AddScoped<ISmsSender, DevelopmentSmsSender>();
        }
        else
        {
            services.AddScoped<ISmsSender, TwilioSmsSender>();
        }
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddSingleton<IJwtAccessTokenValidator, JwtAccessTokenValidator>();
        services.AddSingleton<ICookieManager, CookieManager>();
        services.AddCarshiTowObjectStorage();
        return services;
    }

    public static IServiceCollection AddValidation(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();
        services.AddScoped<ValidationFilter>();
        services.AddScoped<DeviceFingerprintFilter>();
        return services;
    }

    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var settings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>() ?? new JwtSettings();
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    ValidIssuer = settings.Issuer,
                    ValidAudience = settings.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.Key)),
                    NameClaimType = JwtRegisteredClaimNames.Sub,
                    RoleClaimType = ClaimTypes.Role
                };
            });
        return services;
    }
}
