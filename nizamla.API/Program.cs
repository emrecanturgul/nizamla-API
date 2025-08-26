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
using nizamla.Infrastructure.Auth;
using nizamla.Infrastructure.Data;
using nizamla.Infrastructure.Repositories;
using Serilog;
using System.Reflection;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ---------------- Serilog Logging ----------------
builder.Host.UseSerilog((context, config) =>
{
    config
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
        .WriteTo.Seq("http://localhost:5341") // SEQ aktifse bu çalışır
        .WriteTo.PostgreSQL(
            connectionString: context.Configuration.GetConnectionString("DefaultConnection"),
            tableName: "Logs",
            needAutoCreateTable: true
        );
});

// ---------------- JWT Config ----------------
var jwtSection = builder.Configuration.GetSection("Jwt");

var jwtKey = jwtSection["Key"]
    ?? Environment.GetEnvironmentVariable("JWT__KEY")
    ?? throw new InvalidOperationException("Jwt:Key eksik. appsettings.json veya environment variable ile tanımlanmalı.");

if (Encoding.UTF8.GetBytes(jwtKey).Length < 32)
    throw new InvalidOperationException("Jwt:Key 256 bit’ten kısa. En az 32 karakter güçlü bir key girin.");

var issuer = jwtSection["Issuer"] ?? throw new InvalidOperationException("Jwt:Issuer eksik.");
var audience = jwtSection["Audience"] ?? throw new InvalidOperationException("Jwt:Audience eksik.");

builder.Services.Configure<JwtOptions>(opt =>
{
    jwtSection.Bind(opt);
    opt.Key = jwtKey;
});

builder.Services.AddSingleton<IRefreshTokenPolicy>(
    new DefaultRefreshTokenPolicy(TimeSpan.FromDays(60)));

// ---------------- Swagger ----------------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Nizamla API",
        Version = "v1",
        Description = "Görev yönetimi ve kullanıcı kimlik doğrulaması için REST API",
        Contact = new OpenApiContact
        {
            Name = "Nizamla Developer",
            Email = "destek@nizamla.com"
        }
    });

    // JWT için Swagger header
    var securitySchema = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "JWT Bearer Token: 'Bearer {token}'",
        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
    };

    c.AddSecurityDefinition("Bearer", securitySchema);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement {
        { securitySchema, new[] { "Bearer" } }
    });


    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        c.IncludeXmlComments(xmlPath);
});

builder.Services.AddControllers();
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddFluentValidationClientsideAdapters();
builder.Services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();

// ModelState custom error response
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


builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<ITaskRepository, TaskRepository>();
builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<PasswordHasher<User>>();
builder.Services.AddScoped<IJwtService, JwtTokenService>();

// ---------------- JWT Authentication ----------------
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

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
        ValidIssuer = issuer,
        ValidAudience = audience,
        IssuerSigningKey = signingKey,
        ClockSkew = TimeSpan.Zero
    };

   
    options.Events = new JwtBearerEvents
    {
        OnChallenge = context =>
        {
            context.HandleResponse();
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";

            var response = new
            {
                statusCode = 401,
                error = "Yetkisiz erişim",
                details = "Geçerli bir JWT token gerekli."
            };

            return context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response));
        },
        OnForbidden = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/json";

            var response = new
            {
                statusCode = 403,
                error = "Erişim reddedildi",
                details = "Bu kaynağa erişim izniniz yok."
            };

            return context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response));
        }
    };
});

builder.Services.AddAuthorization();

// ---------------- Build App ----------------
var app = builder.Build();

app.UseSerilogRequestLogging(); // HTTP logları
app.UseGlobalExceptionMiddleware(); // Global hata middleware

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
