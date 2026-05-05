using HospitalManagamentSystem.Data;
using HospitalManagamentSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HospitalManagamentSystem.Controllers;

[Authorize(Roles = AppRoles.Admin)]
public class DepartmentsController : Controller
{
    private readonly HospitalRepository _repository;

    public DepartmentsController(HospitalRepository repository)
    {
        _repository = repository;
    }

    public async Task<IActionResult> Index() => View(await _repository.GetDepartmentsAsync());

    public IActionResult Create() => View(new Department { Status = "Active" });

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Department department)
    {
        if (!ModelState.IsValid)
        {
            return View(department);
        }

        await _repository.CreateDepartmentAsync(department);
        TempData["Success"] = "Department created.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var department = await _repository.GetDepartmentAsync(id);
        return department is null ? NotFound() : View(department);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Department department)
    {
        if (!ModelState.IsValid)
        {
            return View(department);
        }

        await _repository.UpdateDepartmentAsync(department);
        TempData["Success"] = "Department updated.";
        return RedirectToAction(nameof(Index));
    }
}
