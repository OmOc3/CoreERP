using System.Text;
using ERP.Application.Admin;
using ERP.Application.Auth;
using ERP.Application.Common.Contracts;
using ERP.Infrastructure.Auditing;
using ERP.Infrastructure.Auth;
using ERP.Infrastructure.BackgroundJobs;
using ERP.Infrastructure.Exports;
using ERP.Infrastructure.Persistence;
using ERP.Infrastructure.Services;
using Hangfire;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace ERP.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ErpOptions>(configuration.GetSection("ERP"));
        services.AddHttpContextAccessor();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection connection string is missing.");
        var databaseProvider = configuration["ERP:DatabaseProvider"]?.Trim().ToLowerInvariant() ?? "sqlserver";
        var hangfireEnabled = configuration.GetValue("ERP:Hangfire:Enabled", true);

        services.AddDbContext<ErpDbContext>(options =>
        {
            if (databaseProvider == "sqlite")
            {
                options.UseSqlite(connectionString);
                return;
            }

            options.UseSqlServer(connectionString);
        });
        services.AddScoped<IErpDbContext>(provider => provider.GetRequiredService<ErpDbContext>());

        services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireNonAlphanumeric = false;
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<ErpDbContext>()
            .AddDefaultTokenProviders();

        var jwtOptions = configuration.GetSection("ERP:Jwt").Get<ErpOptions.JwtOptions>() ?? new ErpOptions.JwtOptions();
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key));
        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidAudience = jwtOptions.Audience,
                    IssuerSigningKey = key,
                    ClockSkew = TimeSpan.FromMinutes(1)
                };
            });

        if (hangfireEnabled)
        {
            if (databaseProvider != "sqlserver")
            {
                throw new InvalidOperationException("Hangfire is only supported with the SQL Server database provider.");
            }

            services.AddHangfire(configuration =>
                configuration.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                    .UseSimpleAssemblyNameTypeSerializer()
                    .UseRecommendedSerializerSettings()
                    .UseSqlServerStorage(connectionString, new SqlServerStorageOptions
                    {
                        PrepareSchemaIfNecessary = true
                    }));
            services.AddHangfireServer();
        }

        services.AddScoped<IClock, SystemClock>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<INumberSequenceService, NumberSequenceService>();
        services.AddScoped<IInventoryPolicy, InventoryPolicy>();
        services.AddScoped<IReportExportService, ReportExportService>();
        services.AddScoped<ILowStockAlertService, LowStockAlertService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserAdministrationService, UserAdministrationService>();

        return services;
    }
}
