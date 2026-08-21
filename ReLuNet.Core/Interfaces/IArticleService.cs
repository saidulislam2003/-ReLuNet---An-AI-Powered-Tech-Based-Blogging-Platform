using ReLuNet.Core.Entities;

namespace ReLuNet.Core.Interfaces;

public interface IArticleService
{
    Task<List<Article>> GetPublishedArticlesAsync();
    Task<Article?> GetArticleBySlugAsync(string slug);
    Task<List<Article>> GetArticlesByAuthorAsync(string authorId);
    Task<Article> CreateArticleAsync(Article article, List<string> tags);
    Task<Article> UpdateArticleAsync(Article article, List<string> tags);
    Task DeleteArticleAsync(int id);
    Task<List<Article>> SearchArticlesAsync(string keyword);
    string GenerateSlug(string title);
    int CalculateReadTime(string content);

    Task ToggleLikeAsync(int articleId, string userId);
    Task ToggleBookmarkAsync(int articleId, string userId);
    Task AddCommentAsync(int articleId, string userId, string content);
    Task DeleteCommentAsync(int commentId, string userId);
    bool IsLikedByUser(Article article, string userId);
    bool IsBookmarkedByUser(Article article, string userId);
}