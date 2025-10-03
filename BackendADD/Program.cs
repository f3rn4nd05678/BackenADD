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

// Registrar Repositorios
builder.Services.AddScoped<ILotteryTypeRepository, LotteryTypeRepository>();
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<ILotteryEventRepository, LotteryEventRepository>();


builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS para Vite
builder.Services.AddCors(opt => {
    opt.AddPolicy("vite", p => p
        .WithOrigins("http://localhost:5173")
        .AllowAnyHeader()
        .AllowAnyMethod());
});

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

app.UseApiErrorHandling();

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("vite");
app.MapControllers();
app.Run();