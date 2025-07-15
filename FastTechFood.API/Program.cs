using FastTechFood.API.Middlewares;
using FastTechFood.Application.Dtos;
using FastTechFood.Application.Interfaces;
using FastTechFood.Application.Services;
using FastTechFood.Domain.Interfaces;
using FastTechFood.Infrastructure.Configurations;
using FastTechFood.Infrastructure.Context;
using FastTechFood.Infrastructure.Interfaces;
using FastTechFood.Infrastructure.Migrations;
using FastTechFood.Infrastructure.Repositories;
using FastTechFood.Infrastructure.Services;
using FastTechFood.Messaging.Consumers;
using FastTechFood.Messaging.Publishers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

var configuration = builder.Configuration;

// Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// OpenTelemetry Metrics + Prometheus Exporter
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics.SetResourceBuilder(ResourceBuilder.CreateDefault()
            .AddService("FastTechFoodAPI"))
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation()
            .AddProcessInstrumentation()
            .AddPrometheusExporter();
    });

// MongoDB
builder.Services.AddSingleton<MongoDbContext>(sp =>
    new MongoDbContext(builder.Configuration));

builder.Services.AddSingleton<IMongoDatabase>(sp =>
{
    var mongoDbContext = sp.GetRequiredService<MongoDbContext>();
    return mongoDbContext.mongoDatabase;
});
BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));

// Repositórios
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();

// JWT
var jwtSettingsSection = builder.Configuration.GetSection("JwtSettings");
builder.Services.Configure<JwtSettings>(jwtSettingsSection);

var jwtSettings = jwtSettingsSection.Get<JwtSettings>();
var key = Encoding.ASCII.GetBytes(jwtSettings.Secret);

builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();

builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IValidationService, ValidationService>();
builder.Services.AddSingleton<IRabbitMQPublisherService, RabbitMQPublisherService>();

builder.Services.AddScoped<IConsumer<CreateOrderDTO>, PedidoConsumerHandler>();

builder.Services.AddHostedService(sp =>
    new RabbitMQConsumerService<CreateOrderDTO>(
        sp.GetRequiredService<IConfiguration>(),
        sp.GetRequiredService<ILogger<RabbitMQConsumerService<CreateOrderDTO>>>(),
        sp.GetRequiredService<IServiceScopeFactory>(),
        queueName: builder.Configuration.GetSection("RabbitMQ")["QueueCreateOrder"] ?? string.Empty
    ));

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

// Autorização
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Customer", policy => policy.RequireRole("Customer"));
    options.AddPolicy("Employee", policy => policy.RequireRole("Employee"));
    options.AddPolicy("KitchenStaff", policy => policy.RequireRole("KitchenStaff"));
    options.AddPolicy("Manager", policy => policy.RequireRole("Manager"));
});

// Health Checks
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

// Logging
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var app = builder.Build();

// Executar Migrations
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

// Pipeline HTTP
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Health Endpoints
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false
});

app.MapPrometheusScrapingEndpoint("/metrics");

// Middleware de tratamento de exceções JWT
app.UseMiddleware<JwtExceptionHandlerMiddleware>();

app.Run();