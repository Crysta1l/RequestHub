using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using RequestHub.Data;
using RequestHub.Interfaces;
using RequestHub.Models;
using RequestHub.Repositories;
using Scalar.AspNetCore;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Register DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Scoped - one instance per HTTP request 
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAccessRequestRepository, AccessRequestRepository>();

builder.Services.AddOpenApi();

// Mapper
builder.Services.AddAutoMapper(cfg => { }, typeof(Program));

// JWT 
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var key = Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(key)
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Admin
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (!context.Users.Any(u => u.Email == "admin@test.com"))  // fixed email
    {
        context.Users.Add(new User
        {
            Email = "admin@test.com",
            HashPassword = BCrypt.Net.BCrypt.HashPassword("admin"),
            Role = "Admin"
        });
        context.SaveChanges();
    }
}

// Html
app.UseDefaultFiles();
app.UseStaticFiles();

// Middleware
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();


// Admin and Approver

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    if (!context.Users.Any(u => u.Email == "approver"))
    {
        context.Users.Add(new User
        {
            Email = "approver",
            HashPassword = BCrypt.Net.BCrypt.HashPassword("approver"),
            Role = "Approver"
        });
    }

    if (!context.Users.Any(u => u.Email == "admin"))
    {
        context.Users.Add(new User
        {
            Email = "admin",
            HashPassword = BCrypt.Net.BCrypt.HashPassword("admin"),
            Role = "Admin"
        });
    }

    context.SaveChanges();
}


//using (var scope = app.Services.CreateScope())
//{
//    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
//    dbContext.Database.Migrate(); // applies all pending migrations automatically
//}

app.Run();