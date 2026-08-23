using ReLuNet.Core.Entities;
using ReLuNet.Core.Interfaces;

namespace ReLuNet.Infrastructure.Services;

/// <summary>
/// Empty AI service — Layer 2 placeholder.
/// Fill in with real OpenAI/Gemini + vector DB logic later.
/// </summary>
public class AIService : IAIService
{
    public Task EmbedArticleAsync(Article article)
    {
        // TODO: Layer 2
        // 1. Send article.Content to OpenAI Embeddings API
        // 2. Store returned vector in pgvector/Pinecone
        // 3. Set article.IsEmbedded = true
        return Task.CompletedTask;
    }

    public Task<List<Article>> SemanticSearchAsync(string query)
    {
        // TODO: Layer 2
        // 1. Embed the query
        // 2. Search vector DB for closest article vectors
        // 3. Return ranked articles
        return Task.FromResult(new List<Article>());
    }

    public Task<string> GenerateRAGResponseAsync(string userQuery)
    {
        // TODO: Layer 2
        // 1. Embed query
        // 2. Find relevant articles via vector search
        // 3. Build prompt with articles as context
        // 4. Send to LLM API
        // 5. Return generated response
        return Task.FromResult(string.Empty);
    }

    public Task<string> SummarizeArticleAsync(Article article)
    {
        // TODO: Layer 2
        // 1. Send article.Content to LLM
        // 2. Return summary
        return Task.FromResult(string.Empty);
    }
}