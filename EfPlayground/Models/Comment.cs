namespace EfPlayground.Models;

public class Comment
{
    public int Id { get; set; }
    public string Body { get; set; } = default!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int TaskItemId { get; set; }
    public TaskItem TaskItem { get; set; } = default!;

    public int AuthorId { get; set; }
    public User Author { get; set; } = default!;
}
