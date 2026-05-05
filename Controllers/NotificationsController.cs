using HospitalManagamentSystem.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HospitalManagamentSystem.Controllers;

[Authorize]
public class NotificationsController : AppController
{
    private readonly HospitalRepository _repository;

    public NotificationsController(HospitalRepository repository)
    {
        _repository = repository;
    }

    public async Task<IActionResult> Index() => View(await _repository.GetNotificationsAsync(CurrentUserId));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Read(Guid id, string? returnUrl = null)
    {
        await _repository.MarkNotificationReadAsync(id, CurrentUserId);
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction(nameof(Index));
    }
}
