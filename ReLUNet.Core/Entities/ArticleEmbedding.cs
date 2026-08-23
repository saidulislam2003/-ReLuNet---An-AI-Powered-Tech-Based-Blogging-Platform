namespace ReLuNet.Core.Entities;

public class ArticleEmbedding
{
    public int Id { get; set; }
    public int ArticleId { get; set; }
    public string Embedding { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Article Article { get; set; } = null!;
}
