using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient();
builder.Services.AddHttpContextAccessor();

// Configure ShareKeys for SSO synchronization with AuthServer and SalesWebClient
var shareKeysPath = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, "..", "ShareKeys"));
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(shareKeysPath))
    .SetApplicationName("Lab789");

// Configure Session for caching JWT Token when calling HRWebAPI
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = ".Lab789HR.Session";
});

// Configure Shared SSO Cookie Authentication across the system
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = "Identity.Application";
    options.DefaultChallengeScheme = "Identity.Application";
    options.DefaultSignInScheme = "Identity.Application";
}).AddCookie("Identity.Application", options =>
{
    options.Cookie.Name = ".Lab789.Authentication";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.Path = "/";
    options.ExpireTimeSpan = TimeSpan.FromDays(1);
    options.SlidingExpiration = true;
    options.Events.OnRedirectToLogin = context =>
    {
        var returnUrl = $"{context.Request.Scheme}://{context.Request.Host}{context.Request.PathBase}{context.Request.Path}{context.Request.QueryString}";
        var loginUrl = "http://localhost:5264/Account/Login?returnUrl=" + Uri.EscapeDataString(returnUrl);
        context.Response.Redirect(loginUrl);
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = context =>
    {
        context.Response.Redirect("http://localhost:5264/Account/AccessDenied");
        return Task.CompletedTask;
    };
});

builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseRouting();

app.UseSession();

app.UseAuthentication();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
