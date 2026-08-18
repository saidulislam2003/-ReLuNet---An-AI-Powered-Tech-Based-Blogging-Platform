using ReLuNet.Core.Entities;

namespace ReLuNet.Core.Interfaces;

/// <summary>
/// Placeholder interface for Layer 2 AI integration.
/// Implement this in Infrastructure/Services/AIService.cs when ready.
/// </summary>
public interface IAIService
{
    /// <summary>
    /// Generate vector embedding for an article.
    /// Called automatically when an article is published.
    /// </summary>
    Task EmbedArticleAsync(Article article);

    /// <summary>
    /// Search articles semantically using natural language query.
    /// Returns ranked list of relevant articles.
    /// </summary>
    Task<List<Article>> SemanticSearchAsync(string query);

    /// <summary>
    /// Generate AI response using RAG pipeline.
    /// Finds relevant articles and generates personalized answer.
    /// </summary>
    Task<string> GenerateRAGResponseAsync(string userQuery);

    /// <summary>
    /// Summarize an article using LLM.
    /// </summary>
    Task<string> SummarizeArticleAsync(Article article);
}