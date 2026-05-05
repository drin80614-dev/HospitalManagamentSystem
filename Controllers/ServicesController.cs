using HospitalManagamentSystem.Data;
using HospitalManagamentSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HospitalManagamentSystem.Controllers;

[Authorize(Roles = AppRoles.Admin)]
public class ServicesController : Controller
{
    private readonly HospitalRepository _repository;

    public ServicesController(HospitalRepository repository)
    {
        _repository = repository;
    }

    public async Task<IActionResult> Index() => View(await _repository.GetServicesAsync());

    public async Task<IActionResult> Create()
    {
        ViewBag.Departments = await _repository.GetDepartmentsAsync(true);
        return View(new ServiceItem());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ServiceItem service)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Departments = await _repository.GetDepartmentsAsync(true);
            return View(service);
        }

        await _repository.CreateServiceAsync(service);
        TempData["Success"] = "Service created.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var service = await _repository.GetServiceAsync(id);
        if (service is null)
        {
            return NotFound();
        }

        ViewBag.Departments = await _repository.GetDepartmentsAsync(true);
        return View(service);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ServiceItem service)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Departments = await _repository.GetDepartmentsAsync(true);
            return View(service);
        }

        await _repository.UpdateServiceAsync(service);
        TempData["Success"] = "Service updated.";
        return RedirectToAction(nameof(Index));
    }
}
