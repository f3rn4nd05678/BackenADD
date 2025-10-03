using BackendADD.Data;
using BackendADD.Middleware;
using BackendADD.Models;
using BackendADD.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var cs = builder.Configuration.GetConnectionString("Default");
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseMySql(cs, ServerVersion.AutoDetect(cs)));

// ===== Registrar TODOS los Repositorios =====
builder.Services.AddScoped<ILotteryTypeRepository, LotteryTypeRepository>();
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<ILotteryEventRepository, LotteryEventRepository>();
builder.Services.AddScoped<IBetRepository, BetRepository>();
builder.Services.AddScoped<IPayoutRepository, PayoutRepository>();
builder.Services.AddScoped<IAppSettingsRepository, AppSettingsRepository>();
builder.Services.AddScoped<IReportRepository, ReportRepository>();

// Servicio en background para gestión automática de estados
builder.Services.AddHostedService<BackendADD.Services.EventStateService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "La Suerte API",
        Version = "v1",
        Description = "API para gestión de sorteos y apuestas"
    });
});

// CORS para Vite
builder.Services.AddCors(opt => {
    opt.AddPolicy("vite", p => p
        .WithOrigins("http://localhost:5173")
        .AllowAnyHeader()
        .AllowAnyMethod());
});

// Configuración de validación de modelos
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(kvp => kvp.Value!.Errors.Count > 0)
                .Select(kvp => new {
                    field = kvp.Key,
                    errors = kvp.Value!.Errors.Select(e => e.ErrorMessage)
                });

            var resp = ApiResponse<object?>.Fail("Validation failed", errors, 400);
            return new ObjectResult(resp) { StatusCode = 400 };
        };
    });

var app = builder.Build();

// Middleware personalizado
app.UseApiErrorHandling();

// Swagger en desarrollo y producción
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "La Suerte API v1");
    c.RoutePrefix = "swagger";
});

app.UseCors("vite");
app.MapControllers();

app.Run();