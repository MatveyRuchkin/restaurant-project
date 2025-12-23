using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using RestaurantAPI.Models;
using RestaurantAPI.Services;
using RestaurantAPI.Middleware;
using RestaurantAPI.Mappings;
using RestaurantAPI.Constants;
using Serilog;
using System.Text;
using System.Text.Json;

// Настройка логирования Serilog
Serilog.Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File(
        path: "Logs/restaurant-api-.txt",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

try
{
    Serilog.Log.Information("Запуск приложения Restaurant API");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog();

    // Подключение базы данных
    builder.Services.AddDbContext<RestaurantDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

    // Регистрация сервисов
    builder.Services.AddScoped<IPasswordService, PasswordService>();
    builder.Services.AddScoped<IBusinessValidationService, BusinessValidationService>();
    builder.Services.AddScoped<IOrderService, OrderService>();
    builder.Services.AddScoped<ICategoryService, CategoryService>();
    builder.Services.AddScoped<IDishService, DishService>();

    // Настройка AutoMapper для маппинга DTO
    builder.Services.AddAutoMapper(typeof(MappingProfile));

    // Настройка CORS для фронтенда
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowFrontend", policy =>
        {
            policy.WithOrigins("http://localhost:3000", "http://localhost:5173", "http://localhost:5175", "http://localhost:4200")
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        });
        });

    // Настройка контроллеров и Swagger
    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.JsonSerializerOptions.WriteIndented = true;
        });
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo 
        { 
            Title = "Restaurant API", 
            Version = "v1" 
        });
        
        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = "JWT Authorization header using the Bearer scheme. Enter your token in the text input below (without 'Bearer' prefix).",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.Http,
            Scheme = "Bearer",
            BearerFormat = "JWT"
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
                    },
                    Scheme = "Bearer",
                    Name = "Bearer",
                    In = ParameterLocation.Header
                },
                Array.Empty<string>()
            }
        });
    });

    // ===== Настройка JWT-аутентификации =====
    var jwtKey = builder.Configuration["Jwt:Key"];
    var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "RestaurantAPI";

    if (string.IsNullOrEmpty(jwtKey))
    {
        throw new InvalidOperationException("JWT Key is not configured in appsettings.json");
    }

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var token = context.Request.Headers["Authorization"].ToString();
                if (string.IsNullOrEmpty(token))
                    return Task.CompletedTask;
                    
                if (!token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    context.Request.Headers["Authorization"] = "Bearer " + token;
                }
                
                return Task.CompletedTask;
            },
            OnAuthenticationFailed = context =>
            {
                context.Response.StatusCode = 401;
                context.Response.ContentType = "application/json";
                
                string message = "Ошибка аутентификации.";
                if (context.Exception is SecurityTokenExpiredException)
                {
                    message = "Ваш токен истёк. Пожалуйста, войдите снова.";
                }
                else if (context.Exception is SecurityTokenInvalidSignatureException)
                {
                    message = "Неверная подпись токена.";
                }
                else if (context.Exception is SecurityTokenInvalidIssuerException)
                {
                    message = "Неверный издатель токена.";
                }
                else if (context.Exception is SecurityTokenInvalidAudienceException)
                {
                    message = "Неверная аудитория токена.";
                }
                
                var result = JsonSerializer.Serialize(new
                {
                    message = message,
                    error = context.Exception?.Message
                });
                return context.Response.WriteAsync(result);
            },
            OnChallenge = context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = 401;
                context.Response.ContentType = "application/json";
                var result = JsonSerializer.Serialize(new
                {
                    message = "Требуется авторизация. Убедитесь, что вы передали токен в заголовке Authorization: Bearer <token>"
                });
                return context.Response.WriteAsync(result);
            }
        };
        
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtIssuer,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.Zero
        };
    });

    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("User", policy => policy.RequireRole(Roles.User, Roles.Waiter, Roles.Admin));
        options.AddPolicy("Waiter", policy => policy.RequireRole(Roles.Waiter, Roles.Admin));
        options.AddPolicy("Admin", policy => policy.RequireRole(Roles.Admin));
    });

    var app = builder.Build();

    // Централизованная обработка исключений (должен быть первым в цепочке middleware)
    app.UseMiddleware<ExceptionHandlingMiddleware>();
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Restaurant API v1");
            c.DisplayRequestDuration();
        });
    }

    app.UseHttpsRedirection();
    app.UseCors("AllowFrontend");

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    Serilog.Log.Information("Приложение Restaurant API успешно запущено");

    app.Run();
}
catch (Exception ex)
{
    Serilog.Log.Fatal(ex, "Приложение завершилось с ошибкой");
    throw;
}
finally
{
    Serilog.Log.CloseAndFlush();
}
