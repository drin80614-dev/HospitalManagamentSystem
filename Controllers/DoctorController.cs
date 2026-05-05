using HospitalManagamentSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HospitalManagamentSystem.Controllers;

[Authorize(Roles = AppRoles.Doctor)]
[Route("Doctor")]
public class DoctorController : Controller
{
    [HttpGet("")]
    [HttpGet("Dashboard")]
    public IActionResult Dashboard() => RedirectToAction("Index", "Dashboard");

    [HttpGet("MyPatients")]
    public IActionResult MyPatients() => RedirectToAction("Index", "Patients");

    [HttpGet("PatientDetails/{id:guid}")]
    public IActionResult PatientDetails(Guid id) => RedirectToAction("Details", "Patients", new { id });

    [HttpGet("AddDiagnosis")]
    public IActionResult AddDiagnosis(Guid? patientId) => RedirectToAction("Create", "Diagnoses", new { patientId });

    [HttpGet("AddVisit")]
    public IActionResult AddVisit(Guid? patientId, Guid? appointmentId) => RedirectToAction("Create", "Visits", new { patientId, appointmentId });

    [HttpGet("CreatePrescription")]
    public IActionResult CreatePrescription(Guid? patientId) => RedirectToAction("Create", "Prescriptions", new { patientId });

    [HttpGet("RequestLabTest")]
    public IActionResult RequestLabTest(Guid? patientId) => RedirectToAction("Create", "LabTests", new { patientId });

    [HttpGet("MyAppointments")]
    public IActionResult MyAppointments() => RedirectToAction("Index", "Appointments", new { date = DateTime.Today });
}
