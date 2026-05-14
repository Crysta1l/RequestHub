using Microsoft.EntityFrameworkCore;
using RequestHub.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Register DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));



builder.Services.AddOpenApi();







var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}






app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
