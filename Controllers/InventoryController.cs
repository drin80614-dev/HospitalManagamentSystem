using HospitalManagamentSystem.Data;
using HospitalManagamentSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HospitalManagamentSystem.Controllers;

[Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Doctor}")]
public class InventoryController : AppController
{
    private readonly HospitalRepository _repository;

    public InventoryController(HospitalRepository repository)
    {
        _repository = repository;
    }

    public async Task<IActionResult> Index(string? search, bool lowStockOnly = false)
        => View(await _repository.GetInventoryAsync(search, lowStockOnly));

    [Authorize(Roles = AppRoles.Admin)]
    public IActionResult Create() => View(new MedicationInventoryItem { Status = "Available", Unit = "pcs", ReorderLevel = 10 });

    [HttpPost, Authorize(Roles = AppRoles.Admin), ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(MedicationInventoryItem item)
    {
        if (!ModelState.IsValid)
        {
            return View(item);
        }

        await _repository.CreateInventoryItemAsync(item, CurrentUserId);
        return FlashAndRedirect("Medication inventory item created.", "Index", "Inventory");
    }

    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Edit(Guid id)
    {
        var item = await _repository.GetInventoryItemAsync(id);
        return item is null ? NotFound() : View(item);
    }

    [HttpPost, Authorize(Roles = AppRoles.Admin), ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(MedicationInventoryItem item)
    {
        if (!ModelState.IsValid)
        {
            return View(item);
        }

        await _repository.UpdateInventoryItemAsync(item, CurrentUserId);
        return FlashAndRedirect("Medication inventory updated.", "Index", "Inventory");
    }
}
