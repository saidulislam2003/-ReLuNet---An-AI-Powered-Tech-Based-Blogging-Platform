namespace ReLuNet.Core.Entities;

public class Comment
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Foreign keys
    public int ArticleId { get; set; }
    public string UserId { get; set; } = string.Empty;

    // Navigation
    public Article Article { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;
}