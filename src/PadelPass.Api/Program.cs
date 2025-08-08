using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using PadelPass.Api.Filters;
using PadelPass.Application;
using PadelPass.Core;
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
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(opt =>
    {
        opt.InvalidModelStateResponseFactory = actionContext =>
        {
            var errors = actionContext.ModelState.SelectMany(x => x.Value.Errors.Select(q => q.ErrorMessage));
            return new BadRequestObjectResult(ApiResponse.Fail(errors));
        };
    });
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
builder.Services.AddCore();
builder.Services.AddHttpContextAccessor();

builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = LocalizationSettings.SupportedCultures;
    options.SetDefaultCulture(LocalizationSettings.DefaultCulture)
        .AddSupportedCultures(supportedCultures)
        .AddSupportedUICultures(supportedCultures);
    options.RequestCultureProviders.Insert(0, new AcceptLanguageHeaderRequestCultureProvider());
});

builder.Services.AddSwaggerGen(c =>
{
    c.OperationFilter<AcceptLanguageHeader>();
    var jwtSecurityScheme = new OpenApiSecurityScheme
    {
        Scheme = "bearer",
        BearerFormat = "JWT",
        Name = "JWT Authentication",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Description = "Put **_ONLY_** your JWT Bearer token on textbox below!",

        Reference = new OpenApiReference
        {
            Id = JwtBearerDefaults.AuthenticationScheme,
            Type = ReferenceType.SecurityScheme
        }
    };

    c.AddSecurityDefinition(jwtSecurityScheme.Reference.Id, jwtSecurityScheme);

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { jwtSecurityScheme, Array.Empty<string>() }
    });
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
app.UseRequestLocalization();
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