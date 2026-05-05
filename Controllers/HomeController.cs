using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using HospitalManagamentSystem.Models;
using Microsoft.AspNetCore.Authorization;

namespace HospitalManagamentSystem.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return User.Identity?.IsAuthenticated == true
            ? RedirectToAction("Index", "Dashboard")
            : RedirectToAction("Login", "Auth");
    }

    [Authorize]
    public IActionResult Privacy()
    {
        return RedirectToAction("Settings", "Admin");
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
