using System.Text;
using System.Text.Json.Serialization;
using DDACAssignment.Data;
using DDACAssignment.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;


var builder = WebApplication.CreateBuilder(args);

// Local Connection String 
var connectionString = builder.Configuration.GetConnectionString("PostgresConnection")
        ?? throw new InvalidOperationException("Connection string not found.");

builder.Services.AddDbContext<DDACDbContext>(options =>
    options.UseNpgsql(connectionString));

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["AppSettings:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["AppSettings:Audience"],
            ValidateLifetime = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["AppSettings:Audience"]!)),
            ValidateIssuerSigningKey = true
        };
    });

    // Configure CORS to allow frontend origin and credentials (for cookies)
    var frontendOrigin = builder.Configuration["Frontend:Url"] ?? "http://localhost:3000";
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowFrontend", policy =>
        {
            policy.WithOrigins(frontendOrigin, "http://localhost:3000", "http://127.0.0.1:3000")
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
    });

// Add Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IApplyService, ApplyService>();
var app = builder.Build();

// Auto-apply migrations on startup
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    logger.LogWarning("=== STARTING MIGRATIONS ===");
    
    var db = scope.ServiceProvider.GetRequiredService<DDACDbContext>();
    var pendingMigrations = db.Database.GetPendingMigrations().ToList();
    
    logger.LogWarning($"Found {pendingMigrations.Count} pending migrations");
    foreach (var migration in pendingMigrations)
    {
        logger.LogWarning($"  - {migration}");
    }
    
    logger.LogWarning("Applying migrations...");
    db.Database.Migrate();
    logger.LogWarning("=== MIGRATIONS COMPLETED ===");
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Enable CORS and authentication middleware
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapGet("/", () => "You are watching the greatest API in the world: DDAC");
app.Run();

// TODO: nit - Recheck the Context lambda initial e.g m => m.MatchId
// TODO: nit - Recheck the variable name for List e.g Tournament instead of Tournaments
// TODO: Add constraints
// TODO: Configure Docker
// TODO: Create Admin
// TODO: Create Request Table
// TODO: Separate HTTP methods into it rightful controller
