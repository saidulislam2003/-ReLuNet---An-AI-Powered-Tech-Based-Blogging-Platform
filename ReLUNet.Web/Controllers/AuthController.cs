using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ReLuNet.Core.Entities;
using ReLuNet.Web.ViewModels;

namespace ReLuNet.Web.Controllers;

public class AuthController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    // GET: /Auth/Register
    [HttpGet]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");

        return View();
    }

    // POST: /Auth/Register
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = new ApplicationUser
        {
            DisplayName = model.DisplayName,
            Email = model.Email,
            UserName = model.Email
        };

        var result = await _userManager.CreateAsync(user, model.Password);

        if (result.Succeeded)
        {
            await _signInManager.SignInAsync(user, isPersistent: false);
            return RedirectToAction("Index", "Home");
        }

        foreach (var error in result.Errors)
            ModelState.AddModelError(string.Empty, error.Description);

        return View(model);
    }

    // GET: /Auth/Login
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");

        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    // POST: /Auth/Login
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await _signInManager.PasswordSignInAsync(
            model.Email,
            model.Password,
            model.RememberMe,
            lockoutOnFailure: false);

        if (result.Succeeded)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index", "Home");
        }

        ModelState.AddModelError(string.Empty, "Invalid email or password");
        return View(model);
    }

    // POST: /Auth/Logout
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    // GET: /Auth/GoogleLogin
    [HttpGet]
    public IActionResult GoogleLogin(string? returnUrl = null)
    {
        var redirectUrl = Url.Action("GoogleCallback", "Auth", new { returnUrl });
        var properties = _signInManager.ConfigureExternalAuthenticationProperties("Google", redirectUrl);
        return Challenge(properties, "Google");
    }

    // GET: /Auth/GoogleCallback
    [HttpGet]
    public async Task<IActionResult> GoogleCallback(string? returnUrl = null)
    {
        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info == null)
            return RedirectToAction("Login");

        // Sign in if already linked
        var result = await _signInManager.ExternalLoginSignInAsync(
            info.LoginProvider,
            info.ProviderKey,
            isPersistent: false);

        if (result.Succeeded)
            return RedirectToAction("Index", "Home");

        // First time Google login — create account
        var email = info.Principal.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
        var name = info.Principal.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;

        if (email == null)
        {
            ModelState.AddModelError(string.Empty, "Could not retrieve email from Google");
            return View("Login");
        }

        var user = new ApplicationUser
        {
            Email = email,
            UserName = email,
            DisplayName = name ?? email
        };

        var createResult = await _userManager.CreateAsync(user);
        if (createResult.Succeeded)
        {
            await _userManager.AddLoginAsync(user, info);
            await _signInManager.SignInAsync(user, isPersistent: false);
            return RedirectToAction("Index", "Home");
        }

        foreach (var error in createResult.Errors)
            ModelState.AddModelError(string.Empty, error.Description);

        return View("Login");
    }
}