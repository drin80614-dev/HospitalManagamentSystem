using HospitalManagamentSystem.Data;
using HospitalManagamentSystem.Models;
using HospitalManagamentSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HospitalManagamentSystem.Controllers;

[Authorize(Roles = AppRoles.Doctor)]
public class DiagnosesController : AppController
{
    public IActionResult Create(Guid? patientId) => RedirectToAction("Create", "Visits", new { patientId });

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Create(ClinicalFormViewModel<Diagnosis> model) => RedirectToAction("Create", "Visits", new { patientId = model.Record.PatientId });
}
