using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using WebApp.Application;
using WebApp.Infrastructure;
using WebApp.Middleware;
using WebApp.Swagger;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    })
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var problemDetails = new ValidationProblemDetails(context.ModelState)
            {
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                Status = StatusCodes.Status400BadRequest,
                Title = "Некорректный запрос",
                Instance = context.HttpContext.Request.Path
            };

            problemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;

            return new BadRequestObjectResult(problemDetails)
            {
                ContentTypes = { "application/problem+json" }
            };
        };
    });

builder.Services.AddProblemDetails();

// JWT: параметры из секции Jwt (appsettings / User Secrets)
var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("В конфигурации не задан Jwt:Secret");
if (Encoding.UTF8.GetByteCount(jwtSecret) < 32)
{
    throw new InvalidOperationException(
        "Jwt:Secret должен быть не короче 32 байт (UTF-8). Задайте секрет через User Secrets или переменные окружения.");
}

var jwtIssuer = builder.Configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException("В конфигурации не задан Jwt:Issuer");
var jwtAudience = builder.Configuration["Jwt:Audience"]
    ?? throw new InvalidOperationException("В конфигурации не задан Jwt:Audience");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            RoleClaimType = System.Security.Claims.ClaimTypes.Role,
            NameClaimType = System.Security.Claims.ClaimTypes.Name
        };

        options.Events = new JwtBearerEvents
        {
            OnChallenge = async context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;

                var problemDetails = new ProblemDetails
                {
                    Type = "https://tools.ietf.org/html/rfc9110#section-15.5.2",
                    Status = StatusCodes.Status401Unauthorized,
                    Title = "Не авторизован",
                    Detail = "Требуется аутентификация",
                    Instance = context.Request.Path
                };
                problemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;

                await context.Response.WriteAsJsonAsync(
                    problemDetails,
                    options: null,
                    contentType: "application/problem+json");
            },
            OnForbidden = async context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;

                var problemDetails = new ProblemDetails
                {
                    Type = "https://tools.ietf.org/html/rfc9110#section-15.5.4",
                    Status = StatusCodes.Status403Forbidden,
                    Title = "Доступ запрещён",
                    Detail = "Недостаточно прав для выполнения операции",
                    Instance = context.Request.Path
                };
                problemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;

                await context.Response.WriteAsJsonAsync(
                    problemDetails,
                    options: null,
                    contentType: "application/problem+json");
            }
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "Events API",
        Version = "v1",
        Description = "Спринт-8. Clean Architecture, JWT и роли Admin/User"
    });

    // кнопка Authorize в Swagger UI
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT в заголовке Authorization. Пример: Bearer {token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    // Bearer только на [Authorize], не на весь документ
    options.OperationFilter<AuthorizeCheckOperationFilter>();

    var presentationXml = Path.Combine(AppContext.BaseDirectory, "WebApp.xml");
    if (File.Exists(presentationXml))
    {
        options.IncludeXmlComments(presentationXml);
    }

    var applicationXml = Path.Combine(AppContext.BaseDirectory, "WebApp.Application.xml");
    if (File.Exists(applicationXml))
    {
        options.IncludeXmlComments(applicationXml);
    }
});

// composition root: слои Application и Infrastructure
builder.Services.AddApplication();
// репозитории и DbContext — Scoped, регистрируются в AddInfrastructure
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

// схема БД через миграции (InMemory в тестах — без Migrate)
app.Services.ApplyMigrations();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }
