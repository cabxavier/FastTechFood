using FastTechFood.API.Middlewares;
using FastTechFood.Application.Interfaces;
using FastTechFood.Application.Services;
using FastTechFood.Domain.Interfaces;
using FastTechFood.Infrastructure.Configurations;
using FastTechFood.Infrastructure.Context;
using FastTechFood.Infrastructure.Interfaces;
using FastTechFood.Infrastructure.Migrations;
using FastTechFood.Infrastructure.Repositories;
using FastTechFood.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using Prometheus;
using System.Text;
using OpenTelemetry.Metrics;

var builder = WebApplication.CreateBuilder(args);

// Configuração básica da aplicação
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configuração do MongoDB
builder.Services.AddSingleton<MongoDbContext>(sp =>
    new MongoDbContext(builder.Configuration));

builder.Services.AddSingleton<IMongoDatabase>(sp =>
{
    var mongoDbContext = sp.GetRequiredService<MongoDbContext>();
    return mongoDbContext.mongoDatabase;
});

BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));

// Registro de repositórios
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();

// Configuração JWT
var jwtSettingsSection = builder.Configuration.GetSection("JwtSettings");
builder.Services.Configure<JwtSettings>(jwtSettingsSection);

var jwtSettings = jwtSettingsSection.Get<JwtSettings>();
var key = Encoding.ASCII.GetBytes(jwtSettings.Secret);

builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();

// Registro de serviços
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IValidationService, ValidationService>();

// Configuração de autenticação
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer("Bearer", options =>
{
    options.RequireHttpsMetadata = true;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidateAudience = true,
        ValidAudience = jwtSettings.Audience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            Console.WriteLine($"Falha na autenticação: {context.Exception}");
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            Console.WriteLine("Token validado com sucesso!");
            Console.WriteLine($"Claims: {string.Join(", ", context.Principal.Claims.Select(c => $"{c.Type}={c.Value}"))}");
            return Task.CompletedTask;
        }
    };
});

// Configuração de autorização
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Customer", policy => policy.RequireRole("Customer"));
    options.AddPolicy("Employee", policy => policy.RequireRole("Employee"));
    options.AddPolicy("KitchenStaff", policy => policy.RequireRole("KitchenStaff"));
    options.AddPolicy("Manager", policy => policy.RequireRole("Manager"));
});

// Configuração de logging
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Configuração de Health Checks e Métricas
// Configuração de Health Checks
builder.Services.AddHealthChecks()
    .AddMongoDb(
        clientFactory: sp => new MongoClient(builder.Configuration.GetConnectionString("MongoDB")),
        databaseNameFactory: sp => "FastTechFood",
        name: "mongodb",
        tags: new[] { "ready" })
    .AddProcessAllocatedMemoryHealthCheck(
        maximumMegabytesAllocated: 500,
        name: "memory",
        tags: new[] { "ready" });

// Configuração de Métricas
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics.AddAspNetCoreInstrumentation()
               .AddHttpClientInstrumentation()
               .AddRuntimeInstrumentation()
               .AddProcessInstrumentation();

        // Configuração correta conforme a assinatura do método
        metrics.AddPrometheusExporter(name: null, configure: options =>
        {
            options.ScrapeEndpointPath = "/metrics";
            options.ScrapeResponseCacheDurationMilliseconds = 0;
        });
    });

var app = builder.Build();

// Execução de migrações
using (var scope = app.Services.CreateScope())
{
    try
    {
        var database = scope.ServiceProvider.GetRequiredService<IMongoDatabase>();
        var migrationRunner = new MongoDbMigrationRunner(database);
        await migrationRunner.RunMigrationsAsync();
        Console.WriteLine("Migrações do MongoDB executadas com sucesso!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Erro ao executar migrações: {ex.Message}");
    }
}

// Configuração do pipeline de requisições HTTP
app.UseSwagger();
    app.UseSwaggerUI();

app.UseHttpsRedirection();

// Configuração de métricas
app.UseMetricServer();
app.UseHttpMetrics(options =>
{
    options.AddCustomLabel("host", context => context.Request.Host.Host);
});

// Mapeamento de endpoints de monitoramento
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.UseMiddleware<JwtExceptionHandlerMiddleware>();

app.Run();