using Microsoft.AspNetCore.Mvc;
using ReLuNet.Core.Interfaces;

namespace ReLuNet.Web.Controllers;

public class ChatController : Controller
{
    private readonly IAIService _aiService;

    public ChatController(IAIService aiService)
    {
        _aiService = aiService;
    }

    // GET: /Chat
    public IActionResult Index()
    {
        return View();
    }

    // POST: /Chat/Ask
    [HttpPost]
    public async Task<IActionResult> Ask([FromBody] ChatRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Query))
            return BadRequest("Query cannot be empty");

        var response = await _aiService.GenerateRAGResponseAsync(request.Query);
        var articles = await _aiService.SemanticSearchAsync(request.Query);

        return Json(new
        {
            response,
            articles = articles.Select(a => new
            {
                a.Title,
                a.Slug,
                a.Summary,
                Author = a.Author.DisplayName,
                a.ReadTime
            })
        });
    }
}

public class ChatRequest
{
    public string Query { get; set; } = string.Empty;
}
