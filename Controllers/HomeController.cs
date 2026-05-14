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
        return View(new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
            StatusCode = 500,
            Title = "Sistemi pati nje problem te perkohshem",
            Message = "Faqja nuk u hap ne kete moment. Rifresko ose kthehu te paneli kryesor; te dhenat nuk humbin."
        });
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Status(int code)
    {
        var isMissingPage = code == StatusCodes.Status404NotFound;
        return View("Error", new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
            StatusCode = code,
            Title = isMissingPage ? "Faqja nuk u gjet" : "Kerkesa nuk mund te perfundohet",
            Message = isMissingPage
                ? "Linku mund te jete i vjeter ose pacienti nuk ekziston me. Kthehu te lista e pacienteve dhe hape kartelen prej aty."
                : "Provoni perseri pas pak ose kthehuni te paneli kryesor."
        });
    }
}
