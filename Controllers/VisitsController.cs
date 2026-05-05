using HospitalManagamentSystem.Data;
using HospitalManagamentSystem.Models;
using HospitalManagamentSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HospitalManagamentSystem.Controllers;

[Authorize(Roles = AppRoles.Doctor)]
public class VisitsController : AppController
{
    private readonly HospitalRepository _repository;

    public VisitsController(HospitalRepository repository)
    {
        _repository = repository;
    }

    public async Task<IActionResult> Create(Guid? patientId, Guid? appointmentId)
    {
        return View(new ClinicalFormViewModel<Visit>
        {
            Record = new Visit
            {
                PatientId = patientId ?? Guid.Empty,
                AppointmentId = appointmentId,
                DoctorId = CurrentDoctorId ?? Guid.Empty,
                VisitDate = DateTime.Now,
                VisitStatus = "Open"
            },
            Patients = await _repository.GetPatientOptionsAsync(CurrentDoctorId),
            Doctors = await _repository.GetDoctorsAsync(true)
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ClinicalFormViewModel<Visit> model)
    {
        model.Record.DoctorId = CurrentDoctorId ?? model.Record.DoctorId;
        if (!ModelState.IsValid)
        {
            model.Patients = await _repository.GetPatientOptionsAsync(CurrentDoctorId);
            model.Doctors = await _repository.GetDoctorsAsync(true);
            return View(model);
        }

        await _repository.CreateVisitAsync(model.Record, CurrentUserId);
        return FlashAndRedirect("Visit record saved.", "Details", "Patients", new { id = model.Record.PatientId });
    }
}
