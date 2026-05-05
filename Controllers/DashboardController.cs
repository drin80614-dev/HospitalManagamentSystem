using HospitalManagamentSystem.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HospitalManagamentSystem.Controllers;

[Authorize]
public class DashboardController : AppController
{
    private readonly HospitalRepository _repository;

    public DashboardController(HospitalRepository repository)
    {
        _repository = repository;
    }

    public async Task<IActionResult> Index()
    {
        var model = await _repository.GetDashboardAsync(CurrentRole, User.Identity?.Name ?? "Team", CurrentDoctorId);
        return View(model);
    }
}
