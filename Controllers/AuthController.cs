using System.Security.Claims;
using HospitalManagamentSystem.Data;
using HospitalManagamentSystem.Models;
using HospitalManagamentSystem.Services;
using HospitalManagamentSystem.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HospitalManagamentSystem.Controllers;

public class AuthController : Controller
{
    private readonly HospitalRepository _repository;
    private readonly IPasswordService _passwordService;

    public AuthController(HospitalRepository repository, IPasswordService passwordService)
    {
        _repository = repository;
        _passwordService = passwordService;
    }

    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Dashboard");
        }

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [AllowAnonymous]
    public IActionResult ForgotPassword() => View(new ForgotPasswordViewModel());

    [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var token = await _repository.CreatePasswordResetTokenAsync(model.Email);
        if (token is not null)
        {
            TempData["ResetLink"] = Url.Action(nameof(ResetPassword), "Auth", new { token }, Request.Scheme);
        }

        TempData["Success"] = "If that email exists, a reset link has been generated.";
        return View(model);
    }

    [AllowAnonymous]
    public IActionResult ResetPassword(string token) => View(new ResetPasswordViewModel { Token = token });

    [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var changed = await _repository.ResetPasswordAsync(model.Token, _passwordService.HashPassword(model.NewPassword));
        if (!changed)
        {
            ModelState.AddModelError(string.Empty, "Reset link is invalid or expired.");
            return View(model);
        }

        TempData["Success"] = "Password reset completed. You can sign in with the new password.";
        return RedirectToAction(nameof(Login));
    }

    [HttpPost, AllowAnonymous, IgnoreAntiforgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _repository.GetUserByLoginAsync(model.Login);
        if (user is null || !_passwordService.VerifyPassword(model.Password, user.PasswordHash))
        {
            ModelState.AddModelError(string.Empty, "Invalid username/email or password.");
            return View(model);
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.RoleName),
            new("username", user.Username)
        };

        if (user.DoctorId.HasValue)
        {
            claims.Add(new Claim("doctor_id", user.DoctorId.Value.ToString()));
        }

        if (user.ReceptionistId.HasValue)
        {
            claims.Add(new Claim("receptionist_id", user.ReceptionistId.Value.ToString()));
        }

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties
            {
                IsPersistent = model.RememberMe,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(model.RememberMe ? 16 : 8)
            });

        await _repository.TouchLastLoginAsync(user.Id);

        if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
        {
            return Redirect(model.ReturnUrl);
        }

        return user.RoleName switch
        {
            AppRoles.Admin => RedirectToAction("Dashboard", "Admin"),
            AppRoles.Doctor => RedirectToAction("Dashboard", "Doctor"),
            AppRoles.Receptionist => RedirectToAction("Dashboard", "Receptionist"),
            _ => RedirectToAction("Index", "Dashboard")
        };
    }

    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    [AllowAnonymous]
    public async Task<IActionResult> ClearSession()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        foreach (var cookie in Request.Cookies.Keys)
        {
            Response.Cookies.Delete(cookie);
        }

        TempData["Success"] = "Session cleared. Sign in again.";
        return RedirectToAction(nameof(Login));
    }

    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        return View();
    }
}
