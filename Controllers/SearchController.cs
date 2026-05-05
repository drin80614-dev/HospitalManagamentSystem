using HospitalManagamentSystem.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HospitalManagamentSystem.Controllers;

[Authorize]
public class SearchController : Controller
{
    private readonly HospitalRepository _repository;

    public SearchController(HospitalRepository repository)
    {
        _repository = repository;
    }

    public async Task<IActionResult> Index(string q)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return RedirectToAction("Index", "Dashboard");
        }

        return View(await _repository.SearchAsync(q));
    }
}
