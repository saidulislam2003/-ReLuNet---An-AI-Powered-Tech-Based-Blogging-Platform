using Microsoft.EntityFrameworkCore;
using ReLuNet.Core.Entities;
using ReLuNet.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using ReLuNet.Application.Services;
using ReLuNet.Core.Interfaces;
using ReLuNet.Infrastructure.Services;
using ReLuNet.Infrastructure.Services;
using ReLuNet.Core.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(
        builder.Configuration.GetConnectionString("DefaultConnection")));

var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection");

Console.WriteLine($"DATABASE: {connectionString}");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionString));

// Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = false;
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

builder.Services.AddAuthentication()
    .AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"]!;
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]!;
    });

// MVC
builder.Services.AddControllersWithViews();
builder.Services.AddScoped<IArticleService, ArticleService>();
builder.Services.AddScoped<IAIService, AIService>();
var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();