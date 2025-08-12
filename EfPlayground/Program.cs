using EfPlayground.Data;
using EfPlayground.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDb>(o =>
    o.UseSqlite(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register seeder
builder.Services.AddTransient<AppDbSeeder>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Migrate + Seed (dev convenience)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDb>();
    await db.Database.MigrateAsync();

    var seeder = scope.ServiceProvider.GetRequiredService<AppDbSeeder>();
    await seeder.SeedAsync(
        userCount: 40,           // tweak as you like
        tasksPerUserMin: 1,
        tasksPerUserMax: 6,
        commentCount: 400,
        friendshipPairs: 150
    );
}

// minimal APIs … (same as before)
app.MapGet("/", () => "EF Core Playground");
app.MapGet("/users", async (AppDb db) => await db.Users.ToListAsync());
// ... other endpoints unchanged

app.Run();
