using HospitalManagamentSystem.Data;
using HospitalManagamentSystem.Models;
using HospitalManagamentSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HospitalManagamentSystem.Controllers;

[Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Doctor}")]
public class LabTestsController : AppController
{
    private readonly HospitalRepository _repository;

    public LabTestsController(HospitalRepository repository)
    {
        _repository = repository;
    }

    public async Task<IActionResult> Index(string? status)
    {
        var doctorId = CurrentRole == AppRoles.Doctor ? CurrentDoctorId : null;
        return View(new LabTestListViewModel { LabTests = await _repository.GetLabTestsAsync(doctorId, status), Status = status });
    }

    public async Task<IActionResult> Create(Guid? patientId)
    {
        return View(new ClinicalFormViewModel<LabTest>
        {
            Record = new LabTest { PatientId = patientId ?? Guid.Empty, DoctorId = CurrentDoctorId ?? Guid.Empty, RequestedDate = DateTime.Today, Status = "Requested" },
            Patients = await _repository.GetPatientOptionsAsync(CurrentDoctorId),
            Doctors = await _repository.GetDoctorsAsync(true)
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ClinicalFormViewModel<LabTest> model)
    {
        model.Record.DoctorId = CurrentDoctorId ?? model.Record.DoctorId;
        if (!ModelState.IsValid)
        {
            model.Patients = await _repository.GetPatientOptionsAsync(CurrentDoctorId);
            model.Doctors = await _repository.GetDoctorsAsync(true);
            return View(model);
        }

        await _repository.CreateLabTestAsync(model.Record, CurrentUserId);
        return FlashAndRedirect("Lab test requested.", "Details", "Patients", new { id = model.Record.PatientId });
    }

    public async Task<IActionResult> Result(Guid id)
    {
        var test = await _repository.GetLabTestAsync(id);
        if (test is null)
        {
            return NotFound();
        }

        if (CurrentRole == AppRoles.Doctor && test.DoctorId != CurrentDoctorId)
        {
            return Forbid();
        }

        return View(new LabResultViewModel
        {
            Id = test.Id,
            PatientName = test.PatientName ?? string.Empty,
            DoctorName = test.DoctorName ?? string.Empty,
            TestName = test.TestName,
            TestType = test.TestType,
            Status = test.Status,
            Result = test.Result,
            ResultDate = test.ResultDate ?? DateTime.Today
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Result(LabResultViewModel model)
    {
        if (model.Status is not ("Requested" or "In Progress" or "Completed"))
        {
            ModelState.AddModelError(nameof(model.Status), "Choose a valid lab status.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        await _repository.UpdateLabResultAsync(model.Id, model.Status, model.Result, model.ResultDate, CurrentUserId);
        return FlashAndRedirect("Lab result updated.", "Index", "LabTests");
    }
}
