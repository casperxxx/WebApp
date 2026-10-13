using System.Reflection;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using WebApp.Application;
using WebApp.Infrastructure;
using WebApp.Middleware;

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
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "Events API",
        Version = "v1",
        Description = "Спринт-7. Clean Architecture: Domain / Application / Infrastructure / Presentation"
    });

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

app.MapControllers();

app.Run();

public partial class Program { }
