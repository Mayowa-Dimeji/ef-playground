namespace EfPlayground.Models;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = default!;

    // 1-to-many: User -> Tasks
    public List<TaskItem> Tasks { get; set; } = new();

    // Many-to-many self reference via Friendship
    public List<Friendship> Friends { get; set; } = new();
    public List<Friendship> FriendOf { get; set; } = new();

    // Comments authored by this user
    public List<Comment> Comments { get; set; } = new();
}
