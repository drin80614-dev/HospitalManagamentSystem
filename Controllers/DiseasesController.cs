using HospitalManagamentSystem.Data;
using HospitalManagamentSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HospitalManagamentSystem.Controllers;

[Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Doctor}")]
public class DiseasesController : Controller
{
    public IActionResult Index() => RedirectToAction("Index", "Dashboard");

    [Authorize(Roles = AppRoles.Admin)]
    public IActionResult Create() => RedirectToAction("Index", "Dashboard");

    [HttpPost, Authorize(Roles = AppRoles.Admin), ValidateAntiForgeryToken]
    public IActionResult Create(Disease disease) => RedirectToAction("Index", "Dashboard");
}
