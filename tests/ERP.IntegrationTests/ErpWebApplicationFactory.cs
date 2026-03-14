using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ERP.Application.Auth;
using ERP.Application.Common.Contracts;
using ERP.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ERP.IntegrationTests;

public sealed class ErpWebApplicationFactory : WebApplicationFactory<Program>, IDisposable
{
    private readonly SqliteConnection _keeperConnection;
    private readonly string _connectionString;
    private readonly Dictionary<string, string?> _originalEnvironment = new();

    public ErpWebApplicationFactory()
    {
        var connectionStringBuilder = new SqliteConnectionStringBuilder
        {
            DataSource = $"erp-integration-{Guid.NewGuid():N}",
            Mode = SqliteOpenMode.Memory,
            Cache = SqliteCacheMode.Shared
        };

        _connectionString = connectionStringBuilder.ToString();
        _keeperConnection = new SqliteConnection(_connectionString);
        _keeperConnection.Open();

        SetEnvironmentOverride("ConnectionStrings__DefaultConnection", _connectionString);
        SetEnvironmentOverride("ERP__DatabaseProvider", "Sqlite");
        SetEnvironmentOverride("ERP__Database__ApplyMigrations", "false");
        SetEnvironmentOverride("ERP__Hangfire__Enabled", "false");
        SetEnvironmentOverride("ERP__UseHttpsRedirection", "false");
        SetEnvironmentOverride("ERP__Jwt__Issuer", "ERP.Test.Api");
        SetEnvironmentOverride("ERP__Jwt__Audience", "ERP.Test.Client");
        SetEnvironmentOverride("ERP__Jwt__Key", "AReallyLongTestingJwtKey12345678901234567890");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = _connectionString,
                ["ERP:DatabaseProvider"] = "Sqlite",
                ["ERP:Database:ApplyMigrations"] = "false",
                ["ERP:Hangfire:Enabled"] = "false",
                ["ERP:UseHttpsRedirection"] = "false",
                ["ERP:Jwt:Issuer"] = "ERP.Test.Api",
                ["ERP:Jwt:Audience"] = "ERP.Test.Client",
                ["ERP:Jwt:Key"] = "AReallyLongTestingJwtKey12345678901234567890"
            });
        });
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<ErpDbContext>>();
            services.RemoveAll<ErpDbContext>();
            services.RemoveAll<IErpDbContext>();

            services.AddDbContext<ErpDbContext>(options => options.UseSqlite(_connectionString));
            services.AddScoped<IErpDbContext>(provider => provider.GetRequiredService<ErpDbContext>());
        });
    }

    public async Task<HttpClient> CreateAuthenticatedClientAsync(
        string userNameOrEmail = "manager",
        string password = "Manager123!",
        CancellationToken cancellationToken = default)
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest
        {
            UserNameOrEmail = userNameOrEmail,
            Password = password
        }, cancellationToken);

        var envelope = await response.ReadAsAsync<TokenEnvelope>(cancellationToken);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", envelope.AccessToken);
        return client;
    }

    public async Task RunLowStockJobAsync(CancellationToken cancellationToken = default)
    {
        using var scope = Services.CreateScope();
        var job = scope.ServiceProvider.GetRequiredService<ILowStockAlertService>();
        await job.GenerateAsync(cancellationToken);
    }

    public new void Dispose()
    {
        foreach (var entry in _originalEnvironment)
        {
            Environment.SetEnvironmentVariable(entry.Key, entry.Value);
        }

        _keeperConnection.Dispose();
        base.Dispose();
    }

    private void SetEnvironmentOverride(string key, string value)
    {
        if (!_originalEnvironment.ContainsKey(key))
        {
            _originalEnvironment[key] = Environment.GetEnvironmentVariable(key);
        }

        Environment.SetEnvironmentVariable(key, value);
    }
}

internal static class HttpResponseMessageExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public static async Task<T> ReadAsAsync<T>(this HttpResponseMessage response, CancellationToken cancellationToken = default)
    {
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        response.IsSuccessStatusCode.Should().BeTrue(
            "{0} {1} for {2} {3}; response body: {4}",
            (int)response.StatusCode,
            response.ReasonPhrase,
            response.RequestMessage?.Method,
            response.RequestMessage?.RequestUri,
            body);
        var payload = await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken);
        payload.Should().NotBeNull();
        return payload!;
    }

    public static async Task EnsureSuccessWithBodyAsync(this HttpResponseMessage response, CancellationToken cancellationToken = default)
    {
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        response.IsSuccessStatusCode.Should().BeTrue(
            "{0} {1} for {2} {3}; response body: {4}",
            (int)response.StatusCode,
            response.ReasonPhrase,
            response.RequestMessage?.Method,
            response.RequestMessage?.RequestUri,
            body);
    }
}
