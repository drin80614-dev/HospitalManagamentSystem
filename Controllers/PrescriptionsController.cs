using HospitalManagamentSystem.Data;
using HospitalManagamentSystem.Models;
using HospitalManagamentSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HospitalManagamentSystem.Controllers;

[Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Doctor}")]
public class PrescriptionsController : AppController
{
    private readonly HospitalRepository _repository;

    public PrescriptionsController(HospitalRepository repository)
    {
        _repository = repository;
    }

    [Authorize(Roles = AppRoles.Doctor)]
    public async Task<IActionResult> Create(Guid? patientId)
    {
        return View(new ClinicalFormViewModel<Prescription>
        {
            Record = new Prescription { PatientId = patientId ?? Guid.Empty, DoctorId = CurrentDoctorId ?? Guid.Empty, PrescriptionDate = DateTime.Today },
            Patients = await _repository.GetPatientOptionsAsync(CurrentDoctorId),
            Doctors = await _repository.GetDoctorsAsync(true)
        });
    }

    [HttpPost, Authorize(Roles = AppRoles.Doctor), ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ClinicalFormViewModel<Prescription> model)
    {
        model.Record.DoctorId = CurrentDoctorId ?? model.Record.DoctorId;
        if (!ModelState.IsValid)
        {
            model.Patients = await _repository.GetPatientOptionsAsync(CurrentDoctorId);
            model.Doctors = await _repository.GetDoctorsAsync(true);
            return View(model);
        }

        var id = await _repository.CreatePrescriptionAsync(model.Record, CurrentUserId);
        return FlashAndRedirect("Prescription created.", "Print", "Prescriptions", new { id });
    }

    public async Task<IActionResult> Print(Guid id)
    {
        var prescription = await _repository.GetPrescriptionAsync(id);
        return prescription is null ? NotFound() : View(prescription);
    }
}
