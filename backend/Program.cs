using System.Text;
using Gmao.Api.Common.Auth;
using Gmao.Api.Common.Configuration;
using Gmao.Api.Common.Db;
using Gmao.Api.Common.Middleware;
using Gmao.Api.Modules.Auth;
using Gmao.Api.Modules.Buses;
using Gmao.Api.Modules.Clients;
using Gmao.Api.Modules.Invoicing;
using Gmao.Api.Modules.Parts;
using Gmao.Api.Modules.Purchasing;
using Gmao.Api.Modules.TimeTracking;
using Gmao.Api.Modules.Users;
using Gmao.Api.Modules.WorkOrders;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException("Configuration JWT manquante.");
var sqlConnectionString = builder.Configuration.GetConnectionString("SqlServer")
    ?? throw new InvalidOperationException("Chaîne de connexion SQL Server manquante.");

builder.Services.AddSingleton(jwtOptions);
builder.Services.AddSingleton<ISqlConnectionFactory>(_ => new SqlConnectionFactory(sqlConnectionString));
builder.Services.AddSingleton<IPasswordHasher, PasswordHasher>();
builder.Services.AddSingleton<IJwtTokenService>(_ => new JwtTokenService(jwtOptions));

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

builder.Services.AddScoped<AuthRepository>();
builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<ClientRepository>();
builder.Services.AddScoped<BusRepository>();
builder.Services.AddScoped<WorkOrderRepository>();
builder.Services.AddScoped<TimeTrackingRepository>();
builder.Services.AddScoped<PartRepository>();
builder.Services.AddScoped<PurchaseOrderRepository>();
builder.Services.AddScoped<InvoicingRepository>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("default", policy =>
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod());
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey)),
            ClockSkew = TimeSpan.FromMinutes(2)
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers();

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseCors("default");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();
