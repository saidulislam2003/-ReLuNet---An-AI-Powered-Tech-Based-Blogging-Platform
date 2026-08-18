using Microsoft.EntityFrameworkCore;
using ReLuNet.Core.Entities;
using ReLuNet.Core.Interfaces;
using ReLuNet.Infrastructure.Data;
using System.Text.RegularExpressions;
using ReLuNet.Core.Interfaces;

namespace ReLuNet.Application.Services;

public class ArticleService : IArticleService
{
    private readonly AppDbContext _context;
    private readonly IAIService _aiService;


    public ArticleService(AppDbContext context, IAIService aiService)
    {
        _context = context;
        _aiService = aiService;
    }

    public async Task<List<Article>> GetPublishedArticlesAsync()
    {
        return await _context.Articles
            .Include(a => a.Author)
            .Include(a => a.ArticleTags)
                .ThenInclude(at => at.Tag)
            .Where(a => a.Status == ArticleStatus.Published)
            .OrderByDescending(a => a.PublishedAt)
            .ToListAsync();
    }

    public async Task<Article?> GetArticleBySlugAsync(string slug)
    {
        return await _context.Articles
            .Include(a => a.Author)
            .Include(a => a.ArticleTags)
                .ThenInclude(at => at.Tag)
            .Include(a => a.Comments)
                .ThenInclude(c => c.User)
            .Include(a => a.Likes)
            .Include(a => a.Bookmarks)
            .FirstOrDefaultAsync(a => a.Slug == slug);
    }

    public async Task<List<Article>> GetArticlesByAuthorAsync(string authorId)
    {
        return await _context.Articles
            .Include(a => a.ArticleTags)
                .ThenInclude(at => at.Tag)
            .Where(a => a.AuthorId == authorId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
    }

    public async Task<Article> CreateArticleAsync(Article article, List<string> tags)
    {
        article.Slug = GenerateSlug(article.Title);
        article.ReadTime = CalculateReadTime(article.Content);
        article.CreatedAt = DateTime.UtcNow;
        article.UpdatedAt = DateTime.UtcNow;

        if (article.Status == ArticleStatus.Published)
            article.PublishedAt = DateTime.UtcNow;

        _context.Articles.Add(article);
        await _context.SaveChangesAsync();

        if (article.Status == ArticleStatus.Published)
            await _aiService.EmbedArticleAsync(article);

        await SyncTagsAsync(article.Id, tags);

        return article;
    }

    public async Task<Article> UpdateArticleAsync(Article article, List<string> tags)
    {
        article.UpdatedAt = DateTime.UtcNow;
        article.ReadTime = CalculateReadTime(article.Content);

        if (article.Status == ArticleStatus.Published && article.PublishedAt == null)
            article.PublishedAt = DateTime.UtcNow;

        // Layer 2 hook — embed if newly published
        if (article.Status == ArticleStatus.Published)
            await _aiService.EmbedArticleAsync(article);
        

        _context.Articles.Update(article);
        await _context.SaveChangesAsync();

        await SyncTagsAsync(article.Id, tags);

        return article;
    }

    public async Task DeleteArticleAsync(int id)
    {
        var article = await _context.Articles.FindAsync(id);
        if (article != null)
        {
            _context.Articles.Remove(article);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<List<Article>> SearchArticlesAsync(string keyword)
    {
        var lower = keyword.ToLower();

        return await _context.Articles
            .Include(a => a.Author)
            .Include(a => a.ArticleTags)
                .ThenInclude(at => at.Tag)
            .Where(a => a.Status == ArticleStatus.Published &&
                (a.Title.ToLower().Contains(lower) ||
                 a.Summary!.ToLower().Contains(lower) ||
                 a.ArticleTags.Any(at => at.Tag.Name.ToLower().Contains(lower))))
            .OrderByDescending(a => a.PublishedAt)
            .ToListAsync();
    }

    public string GenerateSlug(string title)
    {
        var slug = title.ToLower();
        slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");
        slug = Regex.Replace(slug, @"\s+", "-");
        slug = slug.Trim('-');
        slug = $"{slug}-{DateTime.UtcNow.Ticks}";
        return slug;
    }

    public int CalculateReadTime(string content)
    {
        var plainText = Regex.Replace(content, "<.*?>", "");
        var wordCount = plainText.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        return Math.Max(1, wordCount / 200);
    }

    private async Task SyncTagsAsync(int articleId, List<string> tagNames)
    {
        var existing = _context.ArticleTags.Where(at => at.ArticleId == articleId);
        _context.ArticleTags.RemoveRange(existing);
        await _context.SaveChangesAsync();

        foreach (var name in tagNames.Where(t => !string.IsNullOrWhiteSpace(t)))
        {
            var slug = name.ToLower().Trim().Replace(" ", "-");

            var tag = await _context.Tags.FirstOrDefaultAsync(t => t.Slug == slug)
                      ?? new Tag { Name = name.Trim(), Slug = slug };

            if (tag.Id == 0)
            {
                _context.Tags.Add(tag);
                await _context.SaveChangesAsync();
            }

            _context.ArticleTags.Add(new ArticleTag
            {
                ArticleId = articleId,
                TagId = tag.Id
            });
        }

        await _context.SaveChangesAsync();
    }

    public async Task ToggleLikeAsync(int articleId, string userId)
    {
        var existing = await _context.Likes
            .FirstOrDefaultAsync(l => l.ArticleId == articleId && l.UserId == userId);

        if (existing != null)
            _context.Likes.Remove(existing);
        else
            _context.Likes.Add(new Like { ArticleId = articleId, UserId = userId });

        await _context.SaveChangesAsync();
    }

    public async Task ToggleBookmarkAsync(int articleId, string userId)
    {
        var existing = await _context.Bookmarks
            .FirstOrDefaultAsync(b => b.ArticleId == articleId && b.UserId == userId);

        if (existing != null)
            _context.Bookmarks.Remove(existing);
        else
            _context.Bookmarks.Add(new Bookmark { ArticleId = articleId, UserId = userId });

        await _context.SaveChangesAsync();
    }

    public async Task AddCommentAsync(int articleId, string userId, string content)
    {
        var comment = new Comment
        {
            ArticleId = articleId,
            UserId = userId,
            Content = content,
            CreatedAt = DateTime.UtcNow
        };

        _context.Comments.Add(comment);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteCommentAsync(int commentId, string userId)
    {
        var comment = await _context.Comments.FindAsync(commentId);
        if (comment != null && comment.UserId == userId)
        {
            _context.Comments.Remove(comment);
            await _context.SaveChangesAsync();
        }
    }

    public bool IsLikedByUser(Article article, string userId)
    {
        return article.Likes.Any(l => l.UserId == userId);
    }

    public bool IsBookmarkedByUser(Article article, string userId)
    {
        return article.Bookmarks.Any(b => b.UserId == userId);
    }
}