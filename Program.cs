using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Sistema_de_Verificación_IMEI.Data;
using Sistema_de_Verificación_IMEI.Helpers;
using Sistema_de_Verificación_IMEI.Services;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// 1. Cargar configuración según entorno
if (builder.Environment.IsProduction())
{
    builder.Configuration.AddJsonFile("appsettings.Production.json", optional: true, reloadOnChange: true);
}

// 2. Configurar servicios
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 3. Configurar CORS para desarrollo y producción
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            // En desarrollo: permitir localhost
            policy.WithOrigins("http://localhost:3000", "http://localhost:3001", "http://localhost:5173")
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        }
        else
        {
            // En producción: permitir solo tu frontend en Render
            // CAMBIA ESTA URL cuando tengas tu frontend desplegado
            policy.WithOrigins("https://imei-frontend.onrender.com")
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        }
    });
});

// 4. Configurar JWT (con variables de entorno en producción)
var jwtKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY")
    ?? builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("La clave JWT no está configurada");

//if (jwtKey.Length < 32)
//{
 //   throw new InvalidOperationException("La clave JWT debe tener al menos 32 caracteres");
//}

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

        // Solo logging en desarrollo
        if (builder.Environment.IsDevelopment())
        {
            options.Events = new JwtBearerEvents
            {
                OnTokenValidated = context =>
                {
                    var userId = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    var username = context.Principal?.Identity?.Name;
                    var roles = context.Principal?.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();

                    Console.WriteLine($"🔐 Token validado: Usuario={username}, ID={userId}");
                    if (roles != null && roles.Any())
                    {
                        Console.WriteLine($"   Roles encontrados: {string.Join(", ", roles)}");
                    }
                    return Task.CompletedTask;
                },

                OnAuthenticationFailed = context =>
                {
                    Console.WriteLine($"❌ Autenticación fallida: {context.Exception.Message}");
                    return Task.CompletedTask;
                }
            };
        }
    });

// 5. Configurar PostgreSQL con Neon
// Obtener connection string de variables de entorno o appsettings
var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL")
    ?? builder.Configuration.GetConnectionString("NeonConnection");

if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("No se encontró la cadena de conexión a la base de datos");
}

Console.WriteLine($"🔄 Configurando conexión a la base de datos...");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseNpgsql(connectionString);

    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
        options.LogTo(Console.WriteLine, LogLevel.Information);
    }
});

// 6. Registrar servicios personalizados
builder.Services.AddScoped<IVerificacionService, VerificacionService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();

// 7. Configurar logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var app = builder.Build();

// 8. Configurar pipeline HTTP
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    Console.WriteLine("🔧 Modo desarrollo activado");
}
else
{
    Console.WriteLine("🚀 Modo producción activado");

    // En producción, usar middleware de seguridad
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

// 9. Middlewares - ORDEN IMPORTANTE
app.UseHttpsRedirection();
app.UseCors("CorsPolicy");
app.UseAuthentication();
app.UseAuthorization();

// 10. Mapear controladores
app.MapControllers();

// 11. Endpoint de health check para Render
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

// 12. Endpoint de test
app.MapGet("/test", () => "✅ API funcionando!");

// 13. Endpoint de error para producción
app.Map("/error", () => Results.Problem("Ha ocurrido un error en el servidor"));

// 14. Mensaje de inicio
Console.WriteLine($"\n🎉 Aplicación iniciada en modo: {app.Environment.EnvironmentName}");
Console.WriteLine($"🌐 Swagger UI: {(app.Environment.IsDevelopment() ? "https://localhost:5001/swagger" : "No disponible en producción")}");
Console.WriteLine("📱 Login endpoint: POST /api/Auth/login");
Console.WriteLine("🔧 Health check: GET /health");
Console.WriteLine("✅ Test endpoint: GET /test");

app.Run();