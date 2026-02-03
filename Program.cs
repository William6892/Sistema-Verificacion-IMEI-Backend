using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Sistema_de_Verificación_IMEI.Data;
using Sistema_de_Verificación_IMEI.Helpers;
using Sistema_de_Verificación_IMEI.Services;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// 1. Configuración básica
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 2. Configurar CORS para desarrollo y producción
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy =>
    {
        policy.WithOrigins(
            // Desarrollo
            "http://localhost:3000",
            "http://localhost:3001",
            "http://localhost:5173",
            // Producción - tu frontend en Render
            "https://imei-frontend.onrender.com",
            "https://imei-api-p18o.onrender.com"  // Para permitir self-calls si es necesario
        )
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials();
    });
});

// 3. Configurar JWT
var jwtKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY")
    ?? builder.Configuration["Jwt:Key"]
    ?? "MiClaveSecretaSuperSeguraDe64CaracteresParaJWTEnProduccionCambiar"; // Valor por defecto

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "SistemaIMEI",
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "IMEIClient",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            NameClaimType = ClaimTypes.NameIdentifier,
            RoleClaimType = ClaimTypes.Role
        };
    });

// 4. Configurar PostgreSQL
var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL")
    ?? Environment.GetEnvironmentVariable("NeonConnection")
    ?? builder.Configuration.GetConnectionString("NeonConnection")
    ?? builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrEmpty(connectionString))
{
    // Si no hay cadena de conexión, usar SQLite para desarrollo
    connectionString = "Data Source=imei.db";
    Console.WriteLine("⚠️ Usando SQLite local para desarrollo");
}
else
{
    Console.WriteLine("✅ Conectado a PostgreSQL en Neon");
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseNpgsql(connectionString);

    if (builder.Environment.IsDevelopment())
    {
        options.EnableDetailedErrors();
        options.EnableSensitiveDataLogging();
    }
});

// 5. Registrar servicios
builder.Services.AddScoped<IVerificacionService, VerificacionService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IEncryptionService, EncryptionService>(); 

var app = builder.Build();

// 6. Pipeline HTTP
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    Console.WriteLine("🔧 Modo desarrollo activado");
}
else
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
    Console.WriteLine("🚀 Modo producción activado");
}

// 7. Middlewares
app.UseHttpsRedirection();
app.UseCors("CorsPolicy");
app.UseAuthentication();
app.UseAuthorization();

// 8. Mapear endpoints
app.MapControllers();

app.MapGet("/", () => "✅ API Sistema de Verificación IMEI - Funcionando");
app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    service = "imei-api",
    timestamp = DateTime.UtcNow
}));
app.MapGet("/test", () => "✅ API funcionando correctamente");

app.Map("/error", () => Results.Problem("Ha ocurrido un error en el servidor"));

app.Run();