namespace EfPlayground.Models;

// explicit join entity for self-referencing many-to-many
public class Friendship
{
    public int UserId { get; set; }       // who sent/has the friendship
    public User User { get; set; } = default!;

    public int FriendId { get; set; }     // who is the friend
    public User Friend { get; set; } = default!;

    public DateTime Since { get; set; } = DateTime.UtcNow;
}
