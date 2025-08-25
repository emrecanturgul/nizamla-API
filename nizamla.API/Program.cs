using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using nizamla.Api.Middleware;
using nizamla.Application.Interfaces;
using nizamla.Application.Services;
using nizamla.Application.Validators;
using nizamla.Core.Entities;
using nizamla.Core.Interfaces;
using nizamla.Infrastructure.Data;
using nizamla.Infrastructure.Repositories;
using nizamla.Infrastructure.Auth; // JwtOptions, JwtTokenService, IRefreshTokenPolicy
using Serilog;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ------------------- Logging -------------------
builder.Logging.ClearProviders();
builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));
builder.Logging.AddConsole();

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
    .WriteTo.PostgreSQL(
        connectionString: builder.Configuration.GetConnectionString("DefaultConnection"),
        tableName: "Logs",
        needAutoCreateTable: true
    )
    .CreateLogger();

builder.Host.UseSerilog();

// ------------------- Options (JWT) -------------------
// Access token ayarlarını appsettings:Jwt'tan bağla (Refresh burada yok!)
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));

// Refresh token süresi appsettings'ten değil, POLICY'den (ör. 60 gün)
builder.Services.AddSingleton<IRefreshTokenPolicy>(
    new DefaultRefreshTokenPolicy(TimeSpan.FromDays(60))
);

// ------------------- Swagger -------------------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Nizamla API", Version = "v1" });

    var securitySchema = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Enter 'Bearer {token}'",
        Reference = new OpenApiReference
        {
            Type = ReferenceType.SecurityScheme,
            Id = "Bearer"
        }
    };
    c.AddSecurityDefinition("Bearer", securitySchema);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { securitySchema, new[] { "Bearer" } }
    });
});

// ------------------- Controllers + FluentValidation -------------------
builder.Services.AddControllers();
builder.Services.AddFluentValidationAutoValidation();
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(x => x.Value.Errors.Count > 0)
            .Select(x => new
            {
                Field = x.Key,
                Errors = x.Value.Errors.Select(e => e.ErrorMessage)
            });

        var responseObj = new
        {
            statusCode = 400,
            error = "Doğrulama hatası",
            details = errors
        };

        return new BadRequestObjectResult(responseObj)
        {
            ContentTypes = { "application/json" }
        };
    };
});

builder.Services.AddFluentValidationClientsideAdapters();
builder.Services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();

// ------------------- AutoMapper -------------------
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

// ------------------- Database -------------------
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// ------------------- Dependency Injection -------------------
builder.Services.AddScoped<ITaskRepository, TaskRepository>();
builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<PasswordHasher<User>>();
// JwtTokenService artık Infrastructure katmanında
builder.Services.AddScoped<IJwtService, JwtTokenService>();

var jwtSettings = builder.Configuration.GetSection("Jwt");

var keyString = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(keyString))
{
    throw new InvalidOperationException("JWT signing key not configured. Set the 'Jwt__Key' environment variable or user secret.");
}
var key = Encoding.UTF8.GetBytes(keyString);
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSection["Issuer"],
        ValidAudience = jwtSection["Audience"],
        IssuerSigningKey = signingKey,
        ClockSkew = TimeSpan.Zero
    };
});

// ------------------- Authorization -------------------
builder.Services.AddAuthorization();

// ------------------- Build App -------------------
var app = builder.Build();


app.UseGlobalExceptionMiddleware();

// Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();
