using HospitalManagamentSystem.Data;
using HospitalManagamentSystem.Services;
using HospitalManagamentSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HospitalManagamentSystem.Controllers;

[Authorize]
public class AccountController : AppController
{
    private readonly HospitalRepository _repository;
    private readonly IPasswordService _passwordService;

    public AccountController(HospitalRepository repository, IPasswordService passwordService)
    {
        _repository = repository;
        _passwordService = passwordService;
    }

    public async Task<IActionResult> Profile()
    {
        var user = await _repository.GetUserByIdAsync(CurrentUserId);
        if (user is null)
        {
            return NotFound();
        }

        return View(new ProfileViewModel
        {
            Username = user.Username,
            Email = user.Email,
            FullName = user.FullName,
            RoleName = user.RoleName
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(ProfileViewModel model)
    {
        var user = await _repository.GetUserByIdAsync(CurrentUserId);
        if (user is null)
        {
            return NotFound();
        }

        model.Username = user.Username;
        model.Email = user.Email;
        model.FullName = user.FullName;
        model.RoleName = user.RoleName;

        if (string.IsNullOrWhiteSpace(model.CurrentPassword) || string.IsNullOrWhiteSpace(model.NewPassword))
        {
            ModelState.AddModelError(string.Empty, "Enter current and new password.");
            return View(model);
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var changed = await _repository.ChangePasswordAsync(
            CurrentUserId,
            model.CurrentPassword,
            _passwordService.HashPassword(model.NewPassword),
            _passwordService.VerifyPassword);

        if (!changed)
        {
            ModelState.AddModelError(string.Empty, "Current password is not correct.");
            return View(model);
        }

        TempData["Success"] = "Password changed successfully.";
        return RedirectToAction(nameof(Profile));
    }
}
