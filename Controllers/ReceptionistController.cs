using HospitalManagamentSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HospitalManagamentSystem.Controllers;

[Authorize(Roles = AppRoles.Receptionist)]
[Route("Receptionist")]
public class ReceptionistController : Controller
{
    [HttpGet("")]
    [HttpGet("Dashboard")]
    public IActionResult Dashboard() => RedirectToAction("Index", "Dashboard");

    [HttpGet("RegisterPatient")]
    public IActionResult RegisterPatient() => RedirectToAction("Create", "Patients");

    [HttpGet("Patients")]
    public IActionResult Patients() => RedirectToAction("Index", "Patients");

    [HttpGet("CreateAppointment")]
    public IActionResult CreateAppointment(Guid? patientId) => RedirectToAction("Create", "Appointments", new { patientId });

    [HttpGet("AssignRoom")]
    public IActionResult AssignRoom(Guid? patientId) => RedirectToAction("Assign", "Rooms", new { patientId });

    [HttpGet("Payments")]
    public IActionResult Payments(Guid? patientId) => RedirectToAction("Create", "Payments", new { patientId });

    [HttpGet("PrintReceipt")]
    public IActionResult PrintReceipt(Guid paymentId) => RedirectToAction("Receipt", "Payments", new { paymentId });
}
