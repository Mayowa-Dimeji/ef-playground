using Bogus;
using EfPlayground.Models;
using Microsoft.EntityFrameworkCore;

namespace EfPlayground.Data;

public class AppDbSeeder
{
    private readonly AppDb _db;

    public AppDbSeeder(AppDb db) => _db = db;

    public async Task SeedAsync(int userCount = 30, int tasksPerUserMin = 1, int tasksPerUserMax = 5,
                                int commentCount = 300, int friendshipPairs = 120)
    {
        // Only seed if empty (idempotent)
        if (await _db.Users.AnyAsync()) return;

        // Make results reproducible while debugging
        Randomizer.Seed = new Random(42);

        // ---- Users ----
        var userFaker = new Faker<User>()
            .RuleFor(u => u.Username, f => f.Internet.UserName().ToLower());

        // Ensure unique usernames
        var users = new List<User>();
        var usedNames = new HashSet<string>();
        while (users.Count < userCount)
        {
            var u = userFaker.Generate();
            if (usedNames.Add(u.Username)) users.Add(u);
        }

        _db.Users.AddRange(users);
        await _db.SaveChangesAsync();

        // ---- Tasks ----
        var rnd = new Random();
        var tasks = new List<TaskItem>();

        foreach (var u in users)
        {
            int count = rnd.Next(tasksPerUserMin, tasksPerUserMax + 1);
            for (int i = 0; i < count; i++)
            {
                tasks.Add(new TaskItem
                {
                    Title = new Faker().Hacker.Phrase(),
                    IsCompleted = new Faker().Random.Bool(0.3f),
                    UserId = u.Id
                });
            }
        }

        _db.Tasks.AddRange(tasks);
        await _db.SaveChangesAsync();

        // ---- Comments ----
        // random comments on random tasks by random users
        var comments = new List<Comment>();
        var taskIds = tasks.Select(t => t.Id).ToList();
        var userIds = users.Select(u => u.Id).ToList();

        var commentFaker = new Faker<Comment>()
            .RuleFor(c => c.Body, f => f.Lorem.Sentence())
            .RuleFor(c => c.CreatedAt, f => f.Date.RecentOffset(30).UtcDateTime);

        for (int i = 0; i < commentCount; i++)
        {
            comments.Add(new Comment
            {
                Body = commentFaker.Generate().Body,
                CreatedAt = commentFaker.Generate().CreatedAt,
                TaskItemId = taskIds[rnd.Next(taskIds.Count)],
                AuthorId = userIds[rnd.Next(userIds.Count)]
            });
        }

        _db.Comments.AddRange(comments);
        await _db.SaveChangesAsync();

        // ---- Friendships (mutual) ----
        // We’ll create unique, undirected pairs (A,B) once, but insert both directions
        var pairs = new HashSet<(int, int)>();
        var friendships = new List<Friendship>();

        // guard: you can’t have more unique pairs than nC2
        int maxPairs = users.Count * (users.Count - 1) / 2;
        friendshipPairs = Math.Min(friendshipPairs, maxPairs);

        while (pairs.Count < friendshipPairs)
        {
            var a = userIds[rnd.Next(userIds.Count)];
            var b = userIds[rnd.Next(userIds.Count)];
            if (a == b) continue;

            var key = a < b ? (a, b) : (b, a);
            if (!pairs.Add(key)) continue;

            var since = new Faker().Date.PastOffset(2).UtcDateTime;

            // insert both directions to simulate mutual friendship
            friendships.Add(new Friendship { UserId = a, FriendId = b, Since = since });
            friendships.Add(new Friendship { UserId = b, FriendId = a, Since = since });
        }

        _db.Friendships.AddRange(friendships);
        await _db.SaveChangesAsync();
    }
}
