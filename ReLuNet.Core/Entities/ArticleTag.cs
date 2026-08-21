namespace ReLuNet.Core.Entities;

public class ArticleTag
{
    public int ArticleId { get; set; }
    public int TagId { get; set; }

    // Navigation
    public Article Article { get; set; } = null!;
    public Tag Tag { get; set; } = null!;
}