using BackendADD.Data;
using BackendADD.Repositories;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ===================================================
// URLs: en PRODUCCIÓN fija explícitamente; en DEV usa
// launchSettings.json para tener http:5044 y https:7173
// ===================================================
if (!builder.Environment.IsDevelopment())
{
    // Si Nginx termina TLS, deja SOLO HTTP interno:
    builder.WebHost.UseUrls("http://0.0.0.0:5044");

    // Si prefieres que la app también escuche HTTPS directo en el server
    // (requiere cert real), descomenta:
    // builder.WebHost.UseUrls("http://0.0.0.0:5044", "https://0.0.0.0:7173");
}

// ======================
// CORS (solo ORIGINS!)
// ======================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.WithOrigins(
                // Local/Vite
                "http://localhost:5173",
                "https://localhost:5173",
                "http://127.0.0.1:5173",
                "http://192.168.0.4:5173",
                "http://192.168.56.1:5173",

                // IP pública (por si accedes directo)
                "http://54.235.57.150",
                "https://54.235.57.150",

                // Tu dominio (sin paths; NO /dashboard)
                "http://lasuerte.work.gd",
                "https://lasuerte.work.gd"
            )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

// ======================
// Servicios base
// ======================
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ======================
// DB Context
// ======================
var connectionString = builder.Configuration.GetConnectionString("Default");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// ======================
// Repositorios
// ======================
builder.Services.AddScoped<ILotteryTypeRepository, LotteryTypeRepository>();
builder.Services.AddScoped<ILotteryEventRepository, LotteryEventRepository>();
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<IBetRepository, BetRepository>();
builder.Services.AddScoped<IPayoutRepository, PayoutRepository>();
builder.Services.AddScoped<IAppSettingsRepository, AppSettingsRepository>();
builder.Services.AddScoped<IReportRepository, ReportRepository>();

var app = builder.Build();

// ================================================
// Swagger: activo siempre y con endpoint relativo
// (así sirve bien tanto en http como en https)
// ================================================
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "BackendADD v1");
    c.RoutePrefix = "swagger";
});

// Opcional: página de errores detallada en DEV
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

// IMPORTANTE: No fuerces redirección a HTTPS si estás detrás de Nginx,
// porque puede romper llamadas internas http->https.
// app.UseHttpsRedirection();

app.UseCors("AllowAll");     // Antes de Auth
app.UseAuthorization();

app.MapControllers();

app.Run();
