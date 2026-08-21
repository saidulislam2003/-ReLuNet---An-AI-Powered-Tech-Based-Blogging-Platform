namespace ReLuNet.Core.Entities;

public class Like
{
    public int ArticleId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Article Article { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;
}