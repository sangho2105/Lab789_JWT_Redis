using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

public class AccountController : Controller
{
    private readonly SignInManager<IdentityUser> signInManager;
    public AccountController(SignInManager<IdentityUser> signInManager)
    {
        this.signInManager = signInManager;
    }
    private static bool IsAllowedReturnUrl(string? returnUrl)
    {
        if (!Uri.TryCreate(returnUrl, UriKind.Absolute, out var uri))
            return false;
        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            return false;
        if (!string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase))
            return false;
        return uri.Port is 5133 or 5284;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl ?? "/";
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string email, string password,  string? returnUrl = null)
    {
        if(string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            ModelState.AddModelError("", "Email or Password are required");
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }
        var result = await signInManager.PasswordSignInAsync(email, password, isPersistent: false, lockoutOnFailure: true);
        if (result.Succeeded)
        {
            if(IsAllowedReturnUrl(returnUrl))
            {
                return Redirect(returnUrl!);
            }
            return Redirect("/");
        }
        ModelState.AddModelError("", "Invalid Email or Password");
        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task< IActionResult> Logout(string? returnUrl = null)
    {
        await signInManager.SignOutAsync();
        if(IsAllowedReturnUrl(returnUrl))
        {
            return Redirect(returnUrl!);
        }
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        return View();
    }
}