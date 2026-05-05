using HospitalManagamentSystem.Data;
using HospitalManagamentSystem.Models;
using HospitalManagamentSystem.Services;
using HospitalManagamentSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HospitalManagamentSystem.Controllers;

[Authorize(Roles = AppRoles.Admin)]
[Route("Admin")]
public class AdminController : AppController
{
    private readonly HospitalRepository _repository;
    private readonly IPasswordService _passwordService;

    public AdminController(HospitalRepository repository, IPasswordService passwordService)
    {
        _repository = repository;
        _passwordService = passwordService;
    }

    [HttpGet("")]
    [HttpGet("Dashboard")]
    public IActionResult Dashboard() => RedirectToAction("Index", "Dashboard");

    [HttpGet("Users")]
    public async Task<IActionResult> Users()
    {
        return View(new UsersAdminViewModel
        {
            Users = await _repository.GetUsersAsync(),
            Roles = await _repository.GetRolesAsync()
        });
    }

    [HttpPost("Users"), ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateUser(UsersAdminViewModel model)
    {
        var roles = await _repository.GetRolesAsync();
        if (!ModelState.IsValid)
        {
            model.Users = await _repository.GetUsersAsync();
            model.Roles = roles;
            return View("Users", model);
        }

        await _repository.CreateUserAsync(model.NewUser, _passwordService.HashPassword(model.NewUser.Password), CurrentUserId);
        TempData["Success"] = "User account created.";
        return RedirectToAction(nameof(Users));
    }

    [HttpGet("Roles")]
    public async Task<IActionResult> Roles()
    {
        return View(await _repository.GetRolesAsync());
    }

    [HttpGet("Settings")]
    public IActionResult Settings() => View();
}
