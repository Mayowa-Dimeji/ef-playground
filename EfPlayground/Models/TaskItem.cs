namespace EfPlayground.Models;

public class TaskItem
{
    public int Id { get; set; }
    public string Title { get; set; } = default!;
    public bool IsCompleted { get; set; }

    // Owner
    public int UserId { get; set; }
    public User User { get; set; } = default!;

    // Comments on this task
    public List<Comment> Comments { get; set; } = new();
}

