using Amazon.SQS;
using EcommerceOrderProcessing.Data;
using EcommerceOrderProcessing.Repositories.Implementations;
using EcommerceOrderProcessing.Repositories.Interfaces;
using EcommerceOrderProcessing.Services.BackgroundServices;
using EcommerceOrderProcessing.Services.Implementations;
using EcommerceOrderProcessing.Services.Interfaces;
using EcommerceOrderProcessing.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

// Swagger removed to avoid ReflectionTypeLoadException caused by incompatible Swashbuckle packages

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Add Swagger/OpenAPI generation with proper configuration

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "EcommerceOrderProcessing API",
        Version = "1.0.0",
        Description = "E-Commerce Order Processing System API"
    });

    // Add JWT Bearer authentication to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Please enter a valid token",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
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
            new string[] { }
        }
    });
});

// Required to access HttpContext inside background services or DelegatingHandlers
builder.Services.AddHttpContextAccessor();

builder.Services.AddControllers();
builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(
                    builder.Configuration.GetConnectionString("DefaultConnection")));


// Register repositories
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
// Register services

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IOrderService, OrderService>();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key is not configured."))),
        ClockSkew = TimeSpan.FromSeconds(30)
    };

    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            context.Response.StatusCode = 401;
            context.Response.ContentType = "application/json";
            var response = new { message = $"Authentication failed: {context.Exception.Message}" };
            return context.Response.WriteAsJsonAsync(response);
        },
        OnChallenge = context =>
        {
            context.HandleResponse();
            context.Response.StatusCode = 401;
            context.Response.ContentType = "application/json";
            var response = new { message = "Token validation failed. Please provide a valid JWT token." };
            return context.Response.WriteAsJsonAsync(response);
        }
    };
});


// Register AWS Options and SQS Client
builder.Services.AddDefaultAWSOptions(builder.Configuration.GetAWSOptions());
builder.Services.AddAWSService<IAmazonSQS>();

// Register Producer Service
builder.Services.AddScoped<IOrderQueueService, OrderQueueService>();

// Register Consumer Background Worker
builder.Services.AddHostedService<OrderQueueConsumerService>();

// Register the HTTP client delegating handler
builder.Services.AddTransient<CorrelationIdDelegatingHandler>();
builder.Services.AddHttpClient("CorrelatedClient")
    .AddHttpMessageHandler<CorrelationIdDelegatingHandler>();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();

    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "EcommerceOrderProcessing v1");
        c.RoutePrefix = "swagger"; // serve at /swagger
        // Collapse schema models by default
        c.DefaultModelsExpandDepth(-1);
    });
}

app.UseHttpsRedirection();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseRouting();

// Authentication & Authorization must come AFTER Swagger middleware and Routing
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
