using System.Text;
using System.Text.Json.Serialization;
using Banking.API.Middleware;
using Banking.Application;
using Banking.Application.Abstractions;
using Banking.Infrastructure;
using Banking.Infrastructure.Data;
using Banking.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// ---------- Configuration ----------
var connectionString = builder.Configuration.GetConnectionString("BankingDb")
    ?? throw new InvalidOperationException("Connection string 'BankingDb' is missing.");

var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>() ?? new JwtSettings();
if (jwtSettings.Key.Length < 32)
{
    // Fail fast instead of running with a weak or missing signing key.
    throw new InvalidOperationException(
        "Jwt:Key must be at least 32 characters. Set it in appsettings.Development.json, user-secrets or the Jwt__Key environment variable.");
}

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

// ---------- Services ----------
builder.Services.AddApplication();
builder.Services.AddInfrastructure(connectionString, jwtSettings);

builder.Services.AddControllers()
    // Enums travel as text ("Savings") instead of numbers (2).
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Keep claim names exactly as written in the token ("name", "role").
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
            NameClaimType = JwtTokenService.NameClaim,
            RoleClaimType = JwtTokenService.RoleClaim
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy => policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Banking Operations API", Version = "v1" });

    // Adds the "Authorize" button in Swagger UI so you can paste a JWT.
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Paste the token returned by POST /api/auth/login"
    });
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
    });
});

var app = builder.Build();

// ---------- Database (development convenience) ----------
if (app.Environment.IsDevelopment())
{
    // Creates/updates the database from the EF Core migrations and inserts demo data.
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<BankingDbContext>();
    await db.Database.MigrateAsync();
    await DbSeeder.SeedAsync(db, scope.ServiceProvider.GetRequiredService<IPasswordHasher>());

    app.UseSwagger();
    app.UseSwaggerUI();
}

// ---------- HTTP pipeline (order matters) ----------
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
