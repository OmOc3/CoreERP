using ERP.Api.Authorization;
using ERP.Api.Middleware;
using ERP.Application;
using ERP.Application.Common.Security;
using ERP.Infrastructure;
using ERP.Infrastructure.Persistence;
using ERP.Infrastructure.Seed;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
var corsOrigins = builder.Configuration.GetSection("ERP:Cors:AllowedOrigins").Get<string[]>() ?? [];
var useHttpsRedirection = builder.Configuration.GetValue("ERP:UseHttpsRedirection", false);
var hangfireEnabled = builder.Configuration.GetValue("ERP:Hangfire:Enabled", true);
var applyMigrations = builder.Configuration.GetValue("ERP:Database:ApplyMigrations", true);

builder.Host.UseSerilog((context, services, configuration) =>
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console());

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpContextAccessor();
builder.Services.AddProblemDetails();
builder.Services.AddCors(options =>
{
    options.AddPolicy("ClientApp", policy =>
    {
        if (corsOrigins.Length == 0)
        {
            policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
            return;
        }

        policy.WithOrigins(corsOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<DemoDataSeeder>();

builder.Services.AddAuthorization(options =>
{
    foreach (var permission in PermissionCatalog.GetAll())
    {
        options.AddPolicy(permission.Code, policy => policy.RequireClaim("permission", permission.Code));
    }
});

builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = ApiVersionReader.Combine(
        new UrlSegmentApiVersionReader(),
        new HeaderApiVersionReader("x-api-version"));
}).AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "ERP API",
        Version = "v1",
        Description = "Mini ERP modular monolith API"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseMiddleware<ProblemDetailsMiddleware>();
app.UseSwagger();
app.UseSwaggerUI();
if (useHttpsRedirection)
{
    app.UseHttpsRedirection();
}
app.UseCors("ClientApp");
app.UseAuthentication();
app.UseAuthorization();
if (hangfireEnabled)
{
    app.UseHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization = new IDashboardAuthorizationFilter[] { new HangfireDashboardAuthorizationFilter() }
    });
}
app.MapGet("/health", () => Results.Ok(new { status = "ok" }))
    .AllowAnonymous();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ErpDbContext>();
    var hasMigrations = dbContext.Database.GetMigrations().Any();
    if (applyMigrations && dbContext.Database.IsSqlServer() && hasMigrations)
    {
        await dbContext.Database.MigrateAsync();
    }
    else
    {
        await dbContext.Database.EnsureCreatedAsync();
    }

    var seeder = scope.ServiceProvider.GetRequiredService<DemoDataSeeder>();
    await seeder.SeedAsync();

    if (hangfireEnabled)
    {
        RecurringJob.AddOrUpdate<ILowStockAlertService>(
            "low-stock-alerts",
            service => service.GenerateAsync(CancellationToken.None),
            Cron.Hourly);
    }
}

app.Run();

public partial class Program;
