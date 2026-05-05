using HospitalManagamentSystem.Data;
using HospitalManagamentSystem.Models;
using HospitalManagamentSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HospitalManagamentSystem.Controllers;

[Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Receptionist}")]
public class RoomsController : AppController
{
    public IActionResult Index() => RedirectToAction("Index", "Dashboard");

    [Authorize(Roles = AppRoles.Admin)]
    public IActionResult Create() => RedirectToAction("Index", "Dashboard");

    [HttpPost, Authorize(Roles = AppRoles.Admin), ValidateAntiForgeryToken]
    public IActionResult Create(Room room) => RedirectToAction("Index", "Dashboard");

    [Authorize(Roles = AppRoles.Admin)]
    public IActionResult Edit(Guid id) => RedirectToAction("Index", "Dashboard");

    [HttpPost, Authorize(Roles = AppRoles.Admin), ValidateAntiForgeryToken]
    public IActionResult Edit(Room room) => RedirectToAction("Index", "Dashboard");

    public IActionResult Assign(Guid? patientId) => RedirectToAction("Index", "Dashboard");

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Assign(RoomAssignmentViewModel model) => RedirectToAction("Index", "Dashboard");

    public IActionResult Transfer(Guid? patientId) => RedirectToAction("Index", "Dashboard");

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Transfer(RoomTransferViewModel model) => RedirectToAction("Index", "Dashboard");

    public IActionResult Discharge(Guid? patientId) => RedirectToAction("Index", "Dashboard");

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Discharge(PatientDischargeViewModel model) => RedirectToAction("Index", "Dashboard");
}
