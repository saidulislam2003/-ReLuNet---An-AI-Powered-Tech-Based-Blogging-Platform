using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using ReLuNet.Core.Entities;
using ReLuNet.Core.Interfaces;
using ReLuNet.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ReLuNet.Infrastructure.Services;

public class AIService : IAIService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly AppDbContext _context;

    public AIService(IHttpClientFactory httpClientFactory, IConfiguration config, AppDbContext context)
    {
        _httpClient = httpClientFactory.CreateClient();
        _apiKey = config["Gemini:ApiKey"]!;
        _context = context;
        _httpClient.DefaultRequestHeaders.Add("x-goog-api-key", _apiKey);
    }

    public async Task EmbedArticleAsync(Article article)
    {
        try
        {
            var plainText = System.Text.RegularExpressions.Regex.Replace(article.Content, "<.*?>", "");
            var textToEmbed = $"{article.Title}. {article.Summary ?? ""}. {plainText}";

            var embedding = await GetEmbeddingAsync(textToEmbed);
            if (embedding == null) return;

            var existing = await _context.ArticleEmbeddings
                .FirstOrDefaultAsync(e => e.ArticleId == article.Id);

            if (existing != null)
            {
                existing.Embedding = JsonSerializer.Serialize(embedding);
                existing.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                _context.ArticleEmbeddings.Add(new ArticleEmbedding
                {
                    ArticleId = article.Id,
                    Embedding = JsonSerializer.Serialize(embedding),
                    UpdatedAt = DateTime.UtcNow
                });
            }

            article.IsEmbedded = true;
            _context.Articles.Update(article);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Embedding error: {ex.Message}");
        }
    }

    public async Task<List<Article>> SemanticSearchAsync(string query)
    {
        try
        {
            var queryEmbedding = await GetEmbeddingAsync(query);
            if (queryEmbedding == null) return new List<Article>();

            var embeddings = await _context.ArticleEmbeddings
                .Include(e => e.Article)
                    .ThenInclude(a => a.Author)
                .Include(e => e.Article)
                    .ThenInclude(a => a.ArticleTags)
                        .ThenInclude(at => at.Tag)
                .Where(e => e.Article.Status == ArticleStatus.Published)
                .ToListAsync();

            var scored = embeddings
                .Select(e => new
                {
                    Article = e.Article,
                    Score = CosineSimilarity(
                        queryEmbedding,
                        JsonSerializer.Deserialize<float[]>(e.Embedding)!)
                })
                .OrderByDescending(x => x.Score)
                .Take(5)
                .ToList();

            return scored.Select(x => x.Article).ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Semantic search error: {ex.Message}");
            return new List<Article>();
        }
    }

    public async Task<string> GenerateRAGResponseAsync(string userQuery)
    {
        try
        {
            var relevantArticles = await SemanticSearchAsync(userQuery);

            var context = string.Join("\n\n", relevantArticles.Select(a =>
                $"Title: {a.Title}\nAuthor: {a.Author.DisplayName}\nSummary: {a.Summary}\nURL: /Article/Read/{a.Slug}"));

            var prompt = $"""
                You are ReLuNet AI Assistant, a helpful guide for a tech blogging platform.

                User question: {userQuery}

                Relevant articles from ReLuNet:
                {context}

                Based on the articles above, provide a helpful, friendly response.
                - Give a clear answer or learning path
                - Recommend the relevant articles by title with their URLs
                - If no articles match well, give general guidance
                - Keep response concise and structured
                - Use markdown formatting
                """;

            return await GenerateTextAsync(prompt);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"RAG error: {ex.Message}");
            return "Sorry, I encountered an error. Please try again.";
        }
    }

    public async Task<string> SummarizeArticleAsync(Article article)
    {
        try
        {
            var plainText = System.Text.RegularExpressions.Regex.Replace(article.Content, "<.*?>", "");
            var prompt = $"Summarize this article in 3-4 sentences:\n\nTitle: {article.Title}\n\n{plainText}";
            return await GenerateTextAsync(prompt);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Summary error: {ex.Message}");
            return "Could not generate summary.";
        }
    }

    private async Task<float[]?> GetEmbeddingAsync(string text)
    {
        var url = "https://generativelanguage.googleapis.com/v1beta/models/gemini-embedding-001:embedContent";

        var payload = new
        {
            model = "models/gemini-embedding-001",
            content = new
            {
                parts = new[] { new { text } }
            }
        };

        var response = await _httpClient.PostAsJsonAsync(url, payload);
        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Embedding API error {response.StatusCode}: {err}");
            return null;
        }

        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        var values = doc.RootElement
            .GetProperty("embedding")
            .GetProperty("values")
            .EnumerateArray()
            .Select(v => v.GetSingle())
            .ToArray();

        return values;
    }

    private async Task<string> GenerateTextAsync(string prompt)
    {
        var url = "https://generativelanguage.googleapis.com/v1beta/models/gemini-3.6-flash:generateContent";

        var payload = new
        {
            contents = new[]
            {
                new { parts = new[] { new { text = prompt } } }
            }
        };

        var response = await _httpClient.PostAsJsonAsync(url, payload);
        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Generate API error {response.StatusCode}: {err}");
            return "Could not generate response.";
        }

        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        return doc.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString() ?? "No response.";
    }

    private static float CosineSimilarity(float[] a, float[] b)
    {
        var dot = a.Zip(b, (x, y) => x * y).Sum();
        var magA = MathF.Sqrt(a.Sum(x => x * x));
        var magB = MathF.Sqrt(b.Sum(x => x * x));
        return magA == 0 || magB == 0 ? 0 : dot / (magA * magB);
    }
}
