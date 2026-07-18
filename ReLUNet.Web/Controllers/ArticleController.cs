using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReLuNet.Core.Entities;
using ReLuNet.Core.Interfaces;
using ReLuNet.Infrastructure.Data;

namespace ReLuNet.Web.Controllers;

public class ArticleController : Controller
{
    private readonly IArticleService _articleService;
private readonly UserManager<ApplicationUser> _userManager;
private readonly AppDbContext _context;

public ArticleController(
    IArticleService articleService,
    UserManager<ApplicationUser> userManager,
    AppDbContext context)
{
    _articleService = articleService;
    _userManager = userManager;
    _context = context;
}

    // GET: /Article
    public async Task<IActionResult> Index(string? search)
    {
        List<Article> articles;

        if (!string.IsNullOrWhiteSpace(search))
            articles = await _articleService.SearchArticlesAsync(search);
        else
            articles = await _articleService.GetPublishedArticlesAsync();

        ViewData["Search"] = search;
        return View(articles);
    }

    // GET: /Article/Read/slug
    public async Task<IActionResult> Read(string slug)
    {
        var article = await _articleService.GetArticleBySlugAsync(slug);
        if (article == null) return NotFound();
        return View(article);
    }

    // GET: /Article/Create
    [Authorize]
    public IActionResult Create()
    {
        return View();
    }

    // POST: /Article/Create
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        string title,
        string content,
        string? summary,
        string? tags,
        string status,
        IFormFile? coverImage)
    {
        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(content))
        {
            ModelState.AddModelError("", "Title and content are required");
            return View();
        }

        var userId = _userManager.GetUserId(User)!;
        var coverImagePath = await SaveCoverImageAsync(coverImage);

        var article = new Article
        {
            Title = title,
            Content = content,
            Summary = summary,
            CoverImage = coverImagePath,
            AuthorId = userId,
            Status = status == "Published" ? ArticleStatus.Published : ArticleStatus.Draft
        };

        var tagList = tags?.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList() ?? new List<string>();
        await _articleService.CreateArticleAsync(article, tagList);

        return RedirectToAction("MyArticles");
    }

    // GET: /Article/Edit/5
    [Authorize]
    public async Task<IActionResult> Edit(int id)
    {
        var article = await _articleService.GetArticleBySlugAsync(
            (await _articleService.GetArticlesByAuthorAsync(_userManager.GetUserId(User)!))
            .FirstOrDefault(a => a.Id == id)?.Slug ?? "");

        if (article == null) return NotFound();
        if (article.AuthorId != _userManager.GetUserId(User)) return Forbid();

        return View(article);
    }

    // POST: /Article/Edit/5
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        string title,
        string content,
        string? summary,
        string? tags,
        string status,
        IFormFile? coverImage)
    {
        var userId = _userManager.GetUserId(User)!;
        var articles = await _articleService.GetArticlesByAuthorAsync(userId);
        var article = articles.FirstOrDefault(a => a.Id == id);

        if (article == null) return NotFound();
        if (article.AuthorId != userId) return Forbid();

        article.Title = title;
        article.Content = content;
        article.Summary = summary;
        article.Status = status == "Published" ? ArticleStatus.Published : ArticleStatus.Draft;

        if (coverImage != null)
            article.CoverImage = await SaveCoverImageAsync(coverImage);

        var tagList = tags?.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList() ?? new List<string>();
        await _articleService.UpdateArticleAsync(article, tagList);

        return RedirectToAction("MyArticles");
    }

    // POST: /Article/Delete/5
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = _userManager.GetUserId(User)!;
        var articles = await _articleService.GetArticlesByAuthorAsync(userId);
        var article = articles.FirstOrDefault(a => a.Id == id);

        if (article == null) return NotFound();
        if (article.AuthorId != userId) return Forbid();

        await _articleService.DeleteArticleAsync(id);
        return RedirectToAction("MyArticles");
    }

    // GET: /Article/MyArticles
    [Authorize]
    public async Task<IActionResult> MyArticles()
    {
        var userId = _userManager.GetUserId(User)!;
        var articles = await _articleService.GetArticlesByAuthorAsync(userId);
        return View(articles);
    }

    private async Task<string?> SaveCoverImageAsync(IFormFile? file)
    {
        if (file == null || file.Length == 0) return null;

        var uploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
        Directory.CreateDirectory(uploads);

        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        var filePath = Path.Combine(uploads, fileName);

        using var stream = new FileStream(filePath, FileMode.Create);
        await file.CopyToAsync(stream);

        return $"/uploads/{fileName}";
    }
    // POST: /Article/Like/5
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Like(int id, string slug)
    {
        var userId = _userManager.GetUserId(User)!;
        await _articleService.ToggleLikeAsync(id, userId);
        return RedirectToAction("Read", new { slug });
    }

    // POST: /Article/Bookmark/5
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Bookmark(int id, string slug)
    {
        var userId = _userManager.GetUserId(User)!;
        await _articleService.ToggleBookmarkAsync(id, userId);
        return RedirectToAction("Read", new { slug });
    }

    // POST: /Article/Comment
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Comment(int id, string slug, string content)
    {
        if (!string.IsNullOrWhiteSpace(content))
        {
            var userId = _userManager.GetUserId(User)!;
            await _articleService.AddCommentAsync(id, userId, content);
        }
        return RedirectToAction("Read", new { slug });
    }

    // POST: /Article/DeleteComment
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteComment(int commentId, string slug)
    {
        var userId = _userManager.GetUserId(User)!;
        await _articleService.DeleteCommentAsync(commentId, userId);
        return RedirectToAction("Read", new { slug });
    }

    // GET: /Article/Bookmarks
    [Authorize]
    public async Task<IActionResult> Bookmarks()
    {
        var userId = _userManager.GetUserId(User)!;
        var user = await _userManager.FindByIdAsync(userId);

        var bookmarks = await _context.Bookmarks
            .Include(b => b.Article)
                .ThenInclude(a => a.Author)
            .Include(b => b.Article)
                .ThenInclude(a => a.ArticleTags)
                    .ThenInclude(at => at.Tag)
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();

        return View(bookmarks);
    }
}