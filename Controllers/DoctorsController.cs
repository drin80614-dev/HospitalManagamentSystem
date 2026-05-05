using HospitalManagamentSystem.Data;
using HospitalManagamentSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HospitalManagamentSystem.Controllers;

[Authorize(Roles = AppRoles.Admin)]
public class DoctorsController : AppController
{
    private readonly HospitalRepository _repository;

    public DoctorsController(HospitalRepository repository)
    {
        _repository = repository;
    }

    public async Task<IActionResult> Index() => View(await _repository.GetDoctorsAsync());

    public async Task<IActionResult> Create()
    {
        ViewBag.Departments = await _repository.GetDepartmentsAsync(true);
        return View(new Doctor { Status = "Active" });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Doctor doctor)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Departments = await _repository.GetDepartmentsAsync(true);
            return View(doctor);
        }

        await _repository.CreateDoctorAsync(doctor, CurrentUserId);
        return FlashAndRedirect("Doctor profile created.", "Index", "Doctors");
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var doctor = await _repository.GetDoctorAsync(id);
        if (doctor is null)
        {
            return NotFound();
        }

        ViewBag.Departments = await _repository.GetDepartmentsAsync(true);
        return View(doctor);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Doctor doctor)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Departments = await _repository.GetDepartmentsAsync(true);
            return View(doctor);
        }

        await _repository.UpdateDoctorAsync(doctor, CurrentUserId);
        return FlashAndRedirect("Doctor profile updated.", "Index", "Doctors");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _repository.DeleteDoctorAsync(id, CurrentUserId);
        return FlashAndRedirect("Doctor deleted.", "Index", "Doctors");
    }
}
