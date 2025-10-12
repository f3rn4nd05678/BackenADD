using BackendADD.Data;
using BackendADD.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ===================================================
// URLs: en PRODUCCIÓN fija explícitamente; en DEV usa
// launchSettings.json para tener http:5044 y https:7173
// ===================================================
if (!builder.Environment.IsDevelopment())
{
    // Si Nginx termina TLS, deja SOLO HTTP interno:
    builder.WebHost.UseUrls("http://0.0.0.0:5044");
    // Si quieres además HTTPS directo (con cert real), descomenta:
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

                // IP pública
                "http://54.235.57.150",
                "https://54.235.57.150",

                // Dominio
                "http://lasuerte.work.gd",
                "https://lasuerte.work.gd"
            )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials(); // solo si usas cookies/credenciales
    });
});

// ======================
// Servicios base
// ======================
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// ======================
// DB Context (mantén "Default" como en tu appsettings)
// ======================
var connectionString = builder.Configuration.GetConnectionString("Default");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// ======================
// JWT Auth
// ======================
var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key no configurada");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "LaSuerteAPI";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "LaSuerteClient";

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        // Detrás de Nginx en HTTP interno:
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// ======================
// Repositorios
// ======================
builder.Services.AddScoped<IAuthRepository, AuthRepository>();
builder.Services.AddScoped<ILotteryTypeRepository, LotteryTypeRepository>();
builder.Services.AddScoped<ILotteryEventRepository, LotteryEventRepository>();
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<IBetRepository, BetRepository>();
builder.Services.AddScoped<IPayoutRepository, PayoutRepository>();
builder.Services.AddScoped<IAppSettingsRepository, AppSettingsRepository>();
builder.Services.AddScoped<IReportRepository, ReportRepository>();

// ======================
// Swagger (siempre activo) + esquema Bearer
// ======================
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "BackendADD", Version = "v1" });

    var jwtSecurityScheme = new OpenApiSecurityScheme
    {
        Scheme = "bearer",
        BearerFormat = "JWT",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Description = "Autorización JWT con esquema Bearer. Ej: 'Bearer {token}'",
        Reference = new OpenApiReference
        {
            Id = "Bearer",
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

// ================================================
// Swagger: activo siempre y con endpoint relativo
// ================================================
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "BackendADD v1");
    c.RoutePrefix = "swagger";
});

// Opcional: errores detallados solo en DEV
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

// IMPORTANTE: sin HTTPS redirection si estás detrás de Nginx
// app.UseHttpsRedirection();

app.UseCors("AllowAll");     // CORS antes de Auth
app.UseAuthentication();     // Luego autenticación
app.UseAuthorization();      // Luego autorización

app.MapControllers();

app.Run();
