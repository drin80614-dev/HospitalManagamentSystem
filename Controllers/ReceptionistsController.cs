using HospitalManagamentSystem.Data;
using HospitalManagamentSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HospitalManagamentSystem.Controllers;

[Authorize(Roles = AppRoles.Admin)]
public class ReceptionistsController : AppController
{
    private readonly HospitalRepository _repository;

    public ReceptionistsController(HospitalRepository repository)
    {
        _repository = repository;
    }

    public async Task<IActionResult> Index() => View(await _repository.GetReceptionistsAsync());

    public IActionResult Create() => View(new Receptionist { Status = "Active" });

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Receptionist receptionist)
    {
        if (!ModelState.IsValid)
        {
            return View(receptionist);
        }

        await _repository.CreateReceptionistAsync(receptionist, CurrentUserId);
        return FlashAndRedirect("Receptionist profile created.", "Index", "Receptionists");
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var receptionist = await _repository.GetReceptionistAsync(id);
        return receptionist is null ? NotFound() : View(receptionist);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Receptionist receptionist)
    {
        if (!ModelState.IsValid)
        {
            return View(receptionist);
        }

        await _repository.UpdateReceptionistAsync(receptionist, CurrentUserId);
        return FlashAndRedirect("Receptionist profile updated.", "Index", "Receptionists");
    }
}
