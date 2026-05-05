using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace HospitalManagamentSystem.Controllers;

public abstract class AppController : Controller
{
    protected Guid CurrentUserId =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : Guid.Empty;

    protected Guid? CurrentDoctorId =>
        Guid.TryParse(User.FindFirstValue("doctor_id"), out var id) ? id : null;

    protected Guid? CurrentReceptionistId =>
        Guid.TryParse(User.FindFirstValue("receptionist_id"), out var id) ? id : null;

    protected string CurrentRole => User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;

    protected IActionResult FlashAndRedirect(string message, string action, string controller, object? routeValues = null)
    {
        TempData["Success"] = message;
        return RedirectToAction(action, controller, routeValues);
    }

    protected void AddError(string message)
    {
        TempData["Error"] = message;
    }
}
