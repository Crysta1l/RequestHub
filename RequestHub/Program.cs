using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RequestHub.Data;
using RequestHub.Interfaces;
using RequestHub.Models;
using RequestHub.Repositories;
using Scalar.AspNetCore;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAccessRequestRepository, AccessRequestRepository>();

builder.Services.AddOpenApi();
builder.Services.AddAutoMapper(cfg => { }, typeof(Program));

var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var key = Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
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

// Creating db and users
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    context.Database.Migrate(); 

    var usersToSeed = new[]
    {
        new { Email = "admin@test.com",  Password = "admin",    Role = "Admin"     },
        new { Email = "admin",           Password = "admin",    Role = "Admin"     },
        new { Email = "approver",        Password = "approver", Role = "Approver"  },
        new { Email = "requester",       Password = "requester",Role = "Requester" },
    };

    foreach (var u in usersToSeed)
    {
        if (!context.Users.Any(x => x.Email == u.Email))
        {
            context.Users.Add(new User
            {
                Email = u.Email,
                HashPassword = BCrypt.Net.BCrypt.HashPassword(u.Password),
                Role = u.Role
            });
        }
    }
    context.SaveChanges();
}

app.UseDefaultFiles();
app.UseStaticFiles();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();