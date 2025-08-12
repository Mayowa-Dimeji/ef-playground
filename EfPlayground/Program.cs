using EfPlayground.Data;
using EfPlayground.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// --- Services ---
builder.Services.AddDbContext<AppDb>(o =>
    o.UseSqlite(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddTransient<AppDbSeeder>();

var app = builder.Build();

// --- Middleware ---
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.DocumentTitle = "EF Core Playground API";
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
    });
}

// --- DB migrate + seed ---
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDb>();
    await db.Database.MigrateAsync();

    var seeder = scope.ServiceProvider.GetRequiredService<AppDbSeeder>();
    await seeder.SeedAsync(
        userCount: 40,
        tasksPerUserMin: 1,
        tasksPerUserMax: 6,
        commentCount: 400,
        friendshipPairs: 150
    );
}

// --- Minimal APIs with Swagger-friendly metadata ---

app.MapGet("/", () => "EF Core Playground API")
   .WithTags("Root")
   .WithOpenApi(op =>
   {
       op.Summary = "API root";
       op.Description = "Basic health/info endpoint.";
       return op;
   });

// Users
app.MapGet("/users", async (AppDb db) =>
        await db.Users.Include(u => u.Tasks).ToListAsync())
   .WithTags("Users")
   .Produces<List<User>>(StatusCodes.Status200OK)
   .WithOpenApi(op =>
   {
       op.Summary = "List all users";
       op.Description = "Returns all users including their tasks.";
       return op;
   });

app.MapGet("/users/{id:int}", async (int id, AppDb db) =>
    await db.Users.Include(u => u.Tasks)
                  .ThenInclude(t => t.Comments)
                  .FirstOrDefaultAsync(u => u.Id == id) is User u
        ? Results.Ok(u)
        : Results.NotFound())
   .WithTags("Users")
   .Produces<User>(StatusCodes.Status200OK)
   .Produces(StatusCodes.Status404NotFound)
   .WithOpenApi(op =>
   {
       op.Summary = "Get user by ID";
       op.Description = "Returns a specific user with tasks and comments.";
       return op;
   });

app.MapPost("/users", async (User input, AppDb db) =>
{
    db.Users.Add(input);
    await db.SaveChangesAsync();
    return Results.Created($"/users/{input.Id}", input);
})
.WithTags("Users")
.Produces<User>(StatusCodes.Status201Created)
.WithOpenApi(op =>
{
    op.Summary = "Create a new user";
    return op;
});

// Tasks
app.MapGet("/tasks", async (AppDb db) =>
        await db.Tasks.Include(t => t.User).ToListAsync())
   .WithTags("Tasks")
   .Produces<List<TaskItem>>(StatusCodes.Status200OK)
   .WithOpenApi(op =>
   {
       op.Summary = "List all tasks";
       op.Description = "Returns all tasks with their owners.";
       return op;
   });

app.MapPost("/tasks", async (TaskItem task, AppDb db) =>
{
    db.Tasks.Add(task);
    await db.SaveChangesAsync();
    return Results.Created($"/tasks/{task.Id}", task);
})
.WithTags("Tasks")
.Produces<TaskItem>(StatusCodes.Status201Created)
.WithOpenApi(op =>
{
    op.Summary = "Create a new task";
    return op;
});

app.MapPatch("/tasks/{id:int}/toggle", async (int id, AppDb db) =>
{
    var t = await db.Tasks.FindAsync(id);
    if (t is null) return Results.NotFound();
    t.IsCompleted = !t.IsCompleted;
    await db.SaveChangesAsync();
    return Results.Ok(t);
})
.WithTags("Tasks")
.Produces<TaskItem>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status404NotFound)
.WithOpenApi(op =>
{
    op.Summary = "Toggle task completion";
    op.Description = "Switches IsCompleted between true/false.";
    return op;
});

// Comments
app.MapGet("/tasks/{taskId:int}/comments", async (int taskId, AppDb db) =>
        await db.Comments.Where(c => c.TaskItemId == taskId)
                         .Include(c => c.Author)
                         .ToListAsync())
   .WithTags("Comments")
   .Produces<List<Comment>>(StatusCodes.Status200OK)
   .WithOpenApi(op =>
   {
       op.Summary = "Get comments for a task";
       return op;
   });

app.MapPost("/tasks/{taskId:int}/comments", async (int taskId, Comment comment, AppDb db) =>
{
    if (taskId != comment.TaskItemId) return Results.BadRequest("TaskId mismatch.");
    db.Comments.Add(comment);
    await db.SaveChangesAsync();
    return Results.Created($"/tasks/{taskId}/comments/{comment.Id}", comment);
})
.WithTags("Comments")
.Produces<Comment>(StatusCodes.Status201Created)
.Produces(StatusCodes.Status400BadRequest)
.WithOpenApi(op =>
{
    op.Summary = "Add a comment to a task";
    return op;
});

// Friendships
app.MapGet("/users/{id:int}/friends", async (int id, AppDb db) =>
{
    var friends = await db.Friendships
        .Where(f => f.UserId == id)
        .Select(f => f.Friend)
        .ToListAsync();
    return Results.Ok(friends);
})
.WithTags("Friends")
.Produces<List<User>>(StatusCodes.Status200OK)
.WithOpenApi(op =>
{
    op.Summary = "List friends for a user";
    return op;
});

app.Run();
