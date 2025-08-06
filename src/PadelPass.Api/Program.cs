using System.ComponentModel;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.IdentityModel.Tokens;
using PadelPass.Application;
using PadelPass.Core.Common;
using PadelPass.Core.Constants;
using PadelPass.Infrastructure;
using PadelPass.Infrastructure.ExceptionHandling;
using PadelPass.Infrastructure.Identity;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Host.UseSerilog();


// Core Services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


// Identity
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
var jwtSettings = builder.Configuration.GetSection("JwtSettings")
    .Get<JwtSettings>();

// Cross-Cutting
builder.Services.AddHealthChecks()
    .AddDbContextCheck<PadelPassDbContext>();
builder.Services.AddResponseCompression();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(p =>
        p.AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod());
});

// Authentication & Authorization
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Audience = jwtSettings.Audience;
        options.RequireHttpsMetadata = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret))
        };
    });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AppRoles.Admin, p => p.RequireRole(AppRoles.Admin, AppRoles.SuperAdmin));
    options.AddPolicy(AppRoles.User, p => p.RequireRole(AppRoles.User));
});
// Infrastructure & Application Layers
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();
builder.Services.AddHttpContextAccessor();


builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = LocalizationSettings.SupportedCultures;
    options.SetDefaultCulture(LocalizationSettings.DefaultCulture)
        .AddSupportedCultures(supportedCultures)
        .AddSupportedUICultures(supportedCultures);
});

var app = builder.Build();

app.UseMiddleware<ErrorHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseResponseCompression();
app.MapHealthChecks("/health");
app.MapControllers();

try
{
    app.SeedDatabase();
}
catch (Exception e)
{
    Console.WriteLine(e);
    throw;
}
app.Run();