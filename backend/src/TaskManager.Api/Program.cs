using System.Text;
using System.Text.Json.Serialization;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using TaskManager.Api.Data;
using TaskManager.Api.Middleware;
using TaskManager.Api.Models;
using TaskManager.Api.Options;
using TaskManager.Api.Services;
using TaskManager.Api.Validators;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

// Plataformas como Vercel, Render e Railway informam a porta pela variável PORT.
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(port)) builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

builder.Services.AddDbContext<AppDbContext>(o => o.UseNpgsql(ConnectionStringHelper.Resolve(config)));

builder.Services.AddIdentityCore<ApplicationUser>(o =>
    {
        o.Password.RequireNonAlphanumeric = false;
        o.Password.RequiredLength = 6;
        o.User.RequireUniqueEmail = true;
    })
    .AddRoles<Microsoft.AspNetCore.Identity.IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>();

var jwt = config.GetSection("Jwt").Get<JwtOptions>() ?? throw new InvalidOperationException("Seção Jwt ausente.");
if (jwt.Key.Length < 32) throw new InvalidOperationException("Jwt:Key deve ter ao menos 32 caracteres.");
builder.Services.Configure<JwtOptions>(config.GetSection("Jwt"));

// Provedor de autenticação: "Local" (Identity + JWT próprio, padrão) ou "Firebase" (ID token do Firebase Auth).
var useFirebase = string.Equals(config["Auth:Provider"], "Firebase", StringComparison.OrdinalIgnoreCase);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.MapInboundClaims = false;

        if (useFirebase)
        {
            var projectId = config["Firebase:ProjectId"];
            if (string.IsNullOrWhiteSpace(projectId))
                throw new InvalidOperationException("Firebase:ProjectId é obrigatório quando Auth:Provider=Firebase.");

            var issuer = $"https://securetoken.google.com/{projectId}";
            var adminEmails = (config.GetSection("Firebase:AdminEmails").Get<string[]>() ?? [])
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .Select(e => e.Trim().ToLowerInvariant())
                .ToHashSet();

            o.Authority = issuer; // baixa automaticamente as chaves públicas do Google
            o.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true, ValidIssuer = issuer,
                ValidateAudience = true, ValidAudience = projectId,
                ValidateLifetime = true,
                NameClaimType = "name", RoleClaimType = "role"
            };
            o.Events = new JwtBearerEvents
            {
                OnTokenValidated = ctx => FirebaseUserProvisioner.OnTokenValidatedAsync(ctx, adminEmails)
            };
        }
        else
        {
            o.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true, ValidIssuer = jwt.Issuer,
                ValidateAudience = true, ValidAudience = jwt.Audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(1),
                NameClaimType = "name", RoleClaimType = "role"
            };
        }
    });
builder.Services.AddAuthorization();

builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddValidatorsFromAssemblyContaining<TaskRequestValidator>();

builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
    .WithOrigins(config.GetSection("Cors:Origins").Get<string[]>() ?? [])
    .AllowAnyHeader().AllowAnyMethod()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Task Manager API",
        Version = "v1",
        Description = "API de gerenciamento de tarefas. No modo Local, use POST /api/auth/login e informe o token em Authorize. No modo Firebase, informe o ID token do Firebase."
    });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization", Type = SecuritySchemeType.Http, Scheme = "bearer",
        BearerFormat = "JWT", In = ParameterLocation.Header, Description = "Token JWT (sem o prefixo Bearer)."
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseSwagger();
app.UseSwaggerUI();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();
app.MapGet("/health", () => Results.Ok(new { status = "healthy", auth = useFirebase ? "firebase" : "local" })).ExcludeFromDescription();
app.MapControllers();

await DbInitializer.InitializeAsync(app.Services, config, app.Logger);

app.Run();
