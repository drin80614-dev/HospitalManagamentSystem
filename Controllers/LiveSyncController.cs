using HospitalManagamentSystem.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HospitalManagamentSystem.Controllers;

[Authorize]
public class LiveSyncController : Controller
{
    private readonly HospitalRepository _repository;

    public LiveSyncController(HospitalRepository repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<IActionResult> Version()
    {
        var version = await _repository.GetLiveSyncVersionAsync();
        return Json(new { version });
    }
}
