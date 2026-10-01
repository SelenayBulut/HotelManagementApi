using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using HotelManagementApi.Data;
using HotelManagementApi.Services;
using Microsoft.OpenApi.Models;
using Serilog;
using HotelManagementApi.Middleware;

var builder = WebApplication.CreateBuilder(args);

// SERILOG AYARLARI
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File(
        "Logs/hotel-api-.txt",
        rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();


// CONTROLLERS
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler =
            System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;

        options.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter());
    });


// SWAGGER SERVİSLERİ
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc(
        "v1",
        new OpenApiInfo
        {
            Title = "HotelManagementApi",
            Version = "v1"
        });

    // JWT SWAGGER AYARI
    // Swagger'a JWT Bearer Authentication seçeneğini ekler.
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description =
            "JWT Authorization header using the Bearer scheme. " +
            "Example: \"Authorization: Bearer {token}\"",

        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    // Swagger endpoint'lerinin JWT ile yetkilendirilmesini sağlar.
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
            Array.Empty<string>()
        }
    });
});


// DATABASE
// AppDbContext'i Dependency Injection sistemine ekler.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));


// TOKEN SERVICE
builder.Services.AddScoped<TokenService>();

// JWT AUTHENTICATION
// Uygulamanın JWT Bearer Authentication kullanacağını belirtir.
builder.Services.AddAuthentication(
    JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,

                ValidIssuer =
                    builder.Configuration["JwtSettings:Issuer"],

                ValidAudience =
                    builder.Configuration["JwtSettings:Audience"],

                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(
                            builder.Configuration[
                                "JwtSettings:Secret"]!))
            };
    });

// [Authorize] kullanılan endpoint'lerin
// yetkilendirme sistemini aktif eder.
builder.Services.AddAuthorization();

// Yukarıdaki servis ve ayarlarla uygulamayı oluşturur.
var app = builder.Build();

// SWAGGER
// Uygulama Development ortamında çalışıyorsa
// Swagger arayüzünü aktif eder.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


// HTTPS
app.UseHttpsRedirection();

// GLOBAL EXCEPTION MIDDLEWARE
//Middleware
// Uygulamada oluşan beklenmeyen hataları
// merkezi olarak yakalar.
app.UseMiddleware<GlobalExceptionMiddleware>();


// AUTHENTICATION & AUTHORIZATION
// Gelen JWT token'ın geçerli olup olmadığını kontrol eder.
app.UseAuthentication();
app.UseAuthorization();

// CONTROLLERS
// Controller endpoint'lerini uygulamaya bağlar.
app.MapControllers();

// Uygulamayı çalıştırır ve gelen HTTP isteklerini dinlemeye başlar.
app.Run();