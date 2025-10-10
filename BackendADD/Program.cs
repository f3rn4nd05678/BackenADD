using BackendADD.Data;
using BackendADD.Repositories;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ============================================
// CORS - ACTUALIZAR CON LA NUEVA IP
// ============================================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.WithOrigins(
                "http://localhost:5173",
                "http://127.0.0.1:5173",
                "http://192.168.0.4:5173",        // ? TU NUEVA IP
                "http://192.168.56.1:5173"
            )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

// URLs de escucha
builder.WebHost.UseUrls("http://0.0.0.0:5044");

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionString = builder.Configuration.GetConnectionString("Default");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

builder.Services.AddScoped<ILotteryTypeRepository, LotteryTypeRepository>();
builder.Services.AddScoped<ILotteryEventRepository, LotteryEventRepository>();
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<IBetRepository, BetRepository>();
builder.Services.AddScoped<IPayoutRepository, PayoutRepository>();
builder.Services.AddScoped<IAppSettingsRepository, AppSettingsRepository>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseHttpsRedirection();  // ? DEBE ESTAR COMENTADO

app.UseCors("AllowAll");  // ? ANTES de UseAuthorization
app.UseAuthorization();
app.MapControllers();

app.Run();